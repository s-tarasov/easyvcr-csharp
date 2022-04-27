using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EasyVCR.Interfaces;
using EasyVCR.InternalUtilities;
using EasyVCR.RequestElements;

namespace EasyVCR.Handlers
{
    /// <summary>
    ///     A handler that records and replays HTTP requests and responses.
    /// </summary>
    internal class VCRHandler : DelegatingHandler
    {
        private readonly Cassette _cassette;

        private readonly Censors _censors;
        private readonly IInteractionConverter _interactionConverter;
        private readonly MatchRules _matchRules;
        private readonly Mode _mode;
        private readonly bool _useOriginalDelay;
        private readonly TimeSpan? _delay;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VCRHandler" /> class.
        /// </summary>
        /// <param name="innerHandler">Inner handler to also execute on requests.</param>
        /// <param name="cassette">Cassette to use to record/replay requests.</param>
        /// <param name="mode">Mode to operate in.</param>
        /// <param name="advancedSettings">Advanced settings to use during recording/replaying, optional</param>
        internal VCRHandler(HttpMessageHandler innerHandler, Cassette cassette, Mode mode, AdvancedSettings? advancedSettings = null)
        {
            InnerHandler = innerHandler;
            _cassette = cassette;
            _mode = mode;

            _censors = advancedSettings?.Censors ?? new Censors();
            _interactionConverter = advancedSettings?.InteractionConverter ?? new DefaultInteractionConverter();
            _matchRules = advancedSettings?.MatchRules ?? new MatchRules();
            _useOriginalDelay = advancedSettings?.SimulateDelay ?? false;
            _delay = advancedSettings?.ManualDelayTimeSpan ?? TimeSpan.Zero;
        }

        /// <summary>
        ///     Override to alter the request-response behavior.
        ///     Record the request and response to the cassette.
        /// </summary>
        /// <param name="request">HttpRequestMessage object.</param>
        /// <param name="cancellationToken">CancellationToken object.</param>
        /// <returns>HttpResponseMessage object.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            switch (_mode)
            {
                case Mode.Record:
                    // make real request, record response
                    stopwatch.Start();
                    var recordResponse = await base.SendAsync(request, cancellationToken);
                    stopwatch.Stop();
                    await RecordRequestAndResponse(request, recordResponse, stopwatch.Elapsed);
                    return recordResponse;
                case Mode.Replay:
                    // try to get recorded request
                    var replayInteraction = await FindMatchingInteraction(request);
                    if (replayInteraction != null)
                    {
                        // simulate delay if configured
                        await SimulateDelay(replayInteraction, cancellationToken);
                        // found a matching interaction, replay response
                        return replayInteraction.Response.ToHttpResponseMessage(request);
                    }
                    throw new VCRException($"No interaction found for request {request.Method} {request.RequestUri}");
                case Mode.Auto:
                    // try to get recorded request
                    var autoInteraction = await FindMatchingInteraction(request);
                    if (autoInteraction != null)
                    {
                        // simulate delay if configured
                        await SimulateDelay(autoInteraction, cancellationToken);
                        // found a matching interaction, replay response
                        return autoInteraction.Response.ToHttpResponseMessage(request);
                    }
                    //  no matching interaction, make real request, record response
                    stopwatch.Start();
                    var autoResponse = await base.SendAsync(request, cancellationToken);
                    stopwatch.Stop();
                    await RecordRequestAndResponse(request, autoResponse, stopwatch.Elapsed);
                    return autoResponse;
                case Mode.Bypass:
                default:
                    var bypassResponse = await base.SendAsync(request, cancellationToken);
                    return bypassResponse;
            }
        }

        /// <summary>
        ///     Search for an existing interaction that matches the request.
        /// </summary>
        /// <param name="request">HttpRequestMessage request.</param>
        /// <returns>Matching HttpInteraction or null if not found.</returns>
        private async Task<HttpInteraction?> FindMatchingInteraction(HttpRequestMessage request)
        {
            var interactions = _cassette.Read();
            var receivedRequest = await _interactionConverter.ToRequestAsync(request, _censors);

            try
            {
                return interactions.First(i => _matchRules.RequestsMatch(receivedRequest, i.Request));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Record the request and response to the cassette.
        /// </summary>
        /// <param name="request">Request to record to cassette.</param>
        /// <param name="response">Response to record to cassette.</param>
        /// <param name="requestDuration">TimeSpan of original real request</param>
        /// <param name="bypassSearch">Bypass search for existing interaction. Useful if already known that one does not exist.</param>
        private async Task RecordRequestAndResponse(HttpRequestMessage request, HttpResponseMessage response, TimeSpan requestDuration, bool bypassSearch = false)
        {
            await Task.Run(async () =>
            {
                var interactionRequest = await _interactionConverter.ToRequestAsync(request, _censors);
                var interactionResponse = await _interactionConverter.ToResponseAsync(response, _censors);
                var httpInteraction = new HttpInteraction
                {
                    Request = interactionRequest,
                    Response = interactionResponse,
                    RecordedAt = DateTimeOffset.Now,
                    Duration = requestDuration.Milliseconds
                };
                // always overrides an existing interaction
                _cassette.UpdateInteraction(httpInteraction, _matchRules, bypassSearch);
            });
        }

        private async Task SimulateDelay(HttpInteraction interaction, CancellationToken cancellationToken)
        {
            if (_useOriginalDelay)
            {
                await Task.Delay(interaction.Duration, cancellationToken);
            }
            else if (_delay.HasValue)
            {
                await Task.Delay(_delay.Value, cancellationToken);
            }
        }
    }
}