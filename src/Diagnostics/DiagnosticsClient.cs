using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ReFlex.Apps.DeepZoom.Diagnostics;

public class DiagnosticsClient
{
    private static readonly HttpClient Client = new HttpClient();
    
    private static readonly string Url = Properties.Settings.Default.DiagnosticsAddress;

    public async Task<HttpResponseMessage> PostAppDataAsync<T>(T payload)
    {
        return await Client.PostAsJsonAsync(Url, payload);
    }
}