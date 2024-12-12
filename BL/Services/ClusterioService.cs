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
            string[]? res;
            try
            {
                res = await _cluster.GetFromJsonAsync<string[]>(_baseUrl + "instances");
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
                var split = item.Split('|');
                dtos.Add(new InstanceDTO()
                {
                    Name = split[0],
                    InstanceId = split[1],
                    Status = split[2],
                    Port = int.Parse(split[3])
                });
            }

            return dtos.FirstOrDefault(x => x.InstanceId == instanceId);
        }
    }
}
