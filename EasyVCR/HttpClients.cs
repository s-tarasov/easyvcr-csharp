using System.Net.Http;
using EasyVCR.Handlers;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasyVCR
{
    /// <summary>
    ///     EasyVcrHttpClient singleton for EasyVCR.
    /// </summary>
    public static class HttpClients
    {
        /// <summary>
        ///     Get a new EasyVcrHttpClient configured to use cassettes.
        /// </summary>
        /// <param name="cassetteFolder">Folder where cassettes will be stored.</param>
        /// <param name="cassetteName">Name of the cassette to use.</param>
        /// <param name="mode">Mode to operate in (i.e. Record, Replay, Auto, Bypass).</param>
        /// <param name="advancedSettings">AdvancedSettings object to use.</param>
        /// <returns>An EasyVcrHttpClient instance.</returns>
        public static EasyVCRHttpClient NewHttpClient(string cassetteFolder, string cassetteName, Mode mode, AdvancedSettings? advancedSettings = null)
        {
            return NewHttpClientWithHandler(null, cassetteFolder, cassetteName, mode, advancedSettings);
        }

        /// <summary>
        ///     Get a new EasyVcrHttpClient configured to use cassettes.
        /// </summary>
        /// <param name="cassette">Cassette object to use.</param>
        /// <param name="mode">Mode to operate in (i.e. Record, Replay, Auto, Bypass).</param>
        /// <param name="advancedSettings">AdvancedSettings object to use.</param>
        /// <returns>An EasyVcrHttpClient instance.</returns>
        public static EasyVCRHttpClient NewHttpClient(Cassette cassette, Mode mode, AdvancedSettings? advancedSettings = null)
        {
            return NewHttpClientWithHandler(null, cassette, mode, advancedSettings);
        }

        /// <summary>
        ///     Get a new EasyVcrHttpClient configured to use cassettes.
        /// </summary>
        /// <param name="innerHandler">Custom inner handler to execute as part of HTTP requests.</param>
        /// <param name="cassetteFolder">Folder where cassettes will be stored.</param>
        /// <param name="cassetteName">Name of the cassette to use.</param>
        /// <param name="mode">Mode to operate in (i.e. Record, Replay, Auto, Bypass).</param>
        /// <param name="advancedSettings">AdvancedSettings object to use.</param>
        /// <returns>An EasyVcrHttpClient instance.</returns>
        public static EasyVCRHttpClient NewHttpClientWithHandler(HttpMessageHandler? innerHandler, string cassetteFolder, string cassetteName, Mode mode, AdvancedSettings? advancedSettings = null)
        {
            return NewHttpClientWithHandler(innerHandler, new Cassette(cassetteFolder, cassetteName), mode, advancedSettings);
        }

        /// <summary>
        ///     Get a new EasyVcrHttpClient configured to use cassettes.
        /// </summary>
        /// <param name="innerHandler">Custom inner handler to execute as part of HTTP requests.</param>
        /// <param name="cassette">Cassette object to use.</param>
        /// <param name="mode">Mode to operate in (i.e. Record, Replay, Auto, Bypass).</param>
        /// <param name="advancedSettings">AdvancedSettings object to use.</param>
        /// <returns>An EasyVcrHttpClient instance.</returns>
        public static EasyVCRHttpClient NewHttpClientWithHandler(HttpMessageHandler? innerHandler, Cassette cassette, Mode mode, AdvancedSettings? advancedSettings = null)
        {
            innerHandler ??= new HttpClientHandler();
            return new EasyVCRHttpClient(new VCRHandler(innerHandler, cassette, mode, advancedSettings));
        }
    }
}
