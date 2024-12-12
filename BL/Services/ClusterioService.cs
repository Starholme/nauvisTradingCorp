using AutoMapper;
using DAL.Data;
using DTO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BL.Services
{
    public interface IClusterioService
    {
        public Task<InstanceDTO?> GetInstanceStatus(string instanceId);
        public Task<bool> StartInstance(string instanceId);
    }

    public class ClusterioService : IClusterioService
    {
        private readonly ILogger<ClusterioService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly HttpClient _cluster;
        private readonly string _baseUrl;

        public ClusterioService(ILogger<ClusterioService> logger, ApplicationDbContext context, IMapper mapper)
        {
            _logger = logger;
            _context = context;
            _mapper = mapper;
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
    }
}
