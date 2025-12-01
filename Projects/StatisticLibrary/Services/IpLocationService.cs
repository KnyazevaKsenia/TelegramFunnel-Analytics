using System.Net.Http.Json;
using StatisticLibrary.Interfaces;
using StatisticLibrary.Models.StatisticModels;


namespace StatisticLibrary.Services;
public class IpLocationService : IIpLocationService
{
    private readonly HttpClient _httpClient;
    
    
    public IpLocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<LocationInfo> GetLocationAsync(string ipAddress)
    {
        try
        {
            
            if (ipAddress == "127.0.0.1" || ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10."))
                return new LocationInfo { Country = "Local", City = "Local" };
            
            var response = await _httpClient.GetFromJsonAsync<LocationInfo>($"http://ip-api.com/json/{ipAddress}");
            if (response == null)
            {
                return new LocationInfo { Country = "Unknown", City = "Unknown" };
            }
            return response;
        }
        catch (Exception ex)
        {
            
            return new LocationInfo();
        }
    }
}