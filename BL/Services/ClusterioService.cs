using AutoMapper;
using DAL.Data;
using DTO;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace BL.Services
{
    public interface IClusterioService
    {
        public Task<InstanceDTO?> GetInstanceStatus(string instanceId);
        public Task<bool> StartInstance(string instanceId);
        public Task<List<ExportFromInstanceDTO>> GetExportsFromInstances(CancellationToken cancellationToken);
        public Task<List<ExportFromInstanceDTO>> GetImportRequestsFromInstances(CancellationToken cancellationToken);
        public Task SetActualImportsForInstances(Dictionary<int, ImportFullfillmentForInstanceJSON> imports, CancellationToken cancellationToken);
    }

    public class ClusterioService : IClusterioService
    {
        private readonly ILogger<ClusterioService> _logger;
        private readonly HttpClient _cluster;
        private readonly string _baseUrl;

        public ClusterioService(ILogger<ClusterioService> logger)
        {
            _logger = logger;
            _cluster = new HttpClient();
            _baseUrl = "http://localhost:8080/api/nauvis_trading_corporation/";
        }

        public async Task<InstanceDTO?> GetInstanceStatus(string instanceId)
        {
            int intId = int.Parse(instanceId);
            InstanceDTO[]? res;
            try
            {
                res = await _cluster.GetFromJsonAsync<InstanceDTO[]>(_baseUrl + "instances");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to contact cluster: {Ex}", ex);
                return null;
            }

            if (res == null) { return null; }

            List<InstanceDTO> dtos = [];

            foreach (var item in res)
            {
                dtos.Add(item);
            }

            return dtos.FirstOrDefault(x => x.InstanceId == intId);
        }

        public async Task<bool> StartInstance(string instanceId)
        {
            string res;
            try
            {
                res = await _cluster.GetStringAsync(_baseUrl + "startInstance?id=" + instanceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to contact cluster: {Ex}", ex);
                return false;
            }
            if (res == "Starting") return true;
            return false;
        }

        public async Task<List<ExportFromInstanceDTO>> GetExportsFromInstances(CancellationToken cancellationToken)
        {
            List<ExportFromInstanceDTO>? dtos;
            try
            {
                dtos = await _cluster.GetFromJsonAsync<List<ExportFromInstanceDTO>>(_baseUrl + "getExportsFromInstances", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to contact cluster: {Ex}", ex);
                return new List<ExportFromInstanceDTO>();
            }
            if (dtos == null) return new List<ExportFromInstanceDTO>();
            return dtos;
        }

        
        public async Task<List<ExportFromInstanceDTO>> GetImportRequestsFromInstances(CancellationToken cancellationToken)
        {
            List<ExportFromInstanceDTO>? dtos;
            try
            {
                dtos = await _cluster.GetFromJsonAsync<List<ExportFromInstanceDTO>>(_baseUrl + "getImportRequestsFromInstances", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to contact cluster: {Ex}", ex);
                return new List<ExportFromInstanceDTO>();
            }
            if (dtos == null) return new List<ExportFromInstanceDTO>();
            return dtos;
        }

        public async Task SetActualImportsForInstances(Dictionary<int, ImportFullfillmentForInstanceJSON> imports, CancellationToken cancellationToken)
        {
            await _cluster.PostAsJsonAsync(_baseUrl + "setActualImportsForInstances", imports, cancellationToken);
        }
    }
}
