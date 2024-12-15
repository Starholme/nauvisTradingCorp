using AutoMapper;
using DAL.Data;
using DAL.DBO;
using DTO;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BL.Services
{
    public interface IVaultService
    {
        public Task<VaultDTO> GetOrCreateVaultForUser(Guid userId);
        public Task GetExportsFromInstances(CancellationToken stoppingToken);
        public Task GetImportRequestsFromInstances(CancellationToken stoppingToken);
    }

    public class VaultService : IVaultService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IClusterioService _clusterioService;

        public VaultService(ApplicationDbContext context, IMapper mapper, IClusterioService clusterioService)
        {
            _context = context;
            _mapper = mapper;
            _clusterioService = clusterioService;
        }

        public async Task<VaultDTO> GetOrCreateVaultForUser(Guid userId)
        {
            var dto = await GetVaultForUser(userId);
            if (dto == null)
            {
                dto = await CreateVaultForUser(userId);
            }
            return dto;
        }

        public async Task<VaultDTO?> GetVaultForUser(Guid userId)
        {
            var dbo = await _context.Vaults.FirstOrDefaultAsync(x => x.OwnerId == userId);
            if (dbo == null) return null;
            VaultDTO dto = _mapper.Map<VaultDTO>(dbo);
            return dto;
        }

        public async Task<VaultDTO> CreateVaultForUser(Guid userId)
        {
            Vault dbo = new Vault();
            dbo.OwnerId = userId;
            _context.Vaults.Add(dbo);
            await _context.SaveChangesAsync();

            VaultDTO dto = _mapper.Map<VaultDTO>(dbo);
            return dto;
        }

        private async Task<Vault?> GetVaultForInstance(string instanceId)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            var vault = await _context.Vaults.Where(v => v.Instances.Any(i => i.ClusterInstanceId == instanceId)).FirstOrDefaultAsync();
#pragma warning restore CS8604 // Possible null reference argument.
            return vault;
        }

        private async Task<bool> AddItemsToVault(ExportFromInstanceDTO dto)
        {
            var dbo = await GetVaultForInstance(dto.InstanceId.ToString());
            if (dbo == null) return false;

            //Loop items to add
            foreach (var item in dto.Items)
            {
                string name = ((JsonElement)item[0]).ToString();
                int qty = 0;
                ((JsonElement)item[1]).TryGetInt32(out qty);
                var itemStack = new ItemStackDTO() { NameAndQuality = name, Quantity = qty };
                await UpsertItemStack(dbo.Id, itemStack);
            }


            return true;
        }

        private async Task UpsertItemStack(int vaultId, ItemStackDTO itemDto)
        {
            var dbo = await _context.Items.FirstOrDefaultAsync(x => x.VaultId == vaultId && x.Name == itemDto.Name && x.Quality == itemDto.Quality);
            if (dbo == null)
            {
                dbo = new ItemStack()
                {
                    Name = itemDto.Name,
                    Quality = itemDto.Quality,
                    Quantity = itemDto.Quantity,
                    VaultId = vaultId,
                };
                _context.Items.Add(dbo);
            }
            else dbo.Quantity += itemDto.Quantity;
        }

        private async Task<int> DecrementItemStack(int vaultId, ItemStackDTO itemDto)
        {
            var dbo = await _context.Items.FirstOrDefaultAsync(x => x.VaultId == vaultId && x.Name == itemDto.Name && x.Quality == itemDto.Quality);
            if (dbo == null)
            {
                return 0;
            }

            int qty = itemDto.Quantity;
            if (dbo.Quantity - qty > -1)
            {
                dbo.Quantity -= qty;
                return qty;
            }
            else
            {
                qty = dbo.Quantity;
                dbo.Quantity = 0;
                return qty;
            }

        }

        public async Task GetExportsFromInstances(CancellationToken stoppingToken)
        {
            //Ask cluster for latest items exported
            var items = await _clusterioService.GetExportsFromInstances(stoppingToken);

            //Add the items
            foreach (var item in items)
            {
                await AddItemsToVault(item);
            }

            await _context.SaveChangesAsync();
        }


        public async Task GetImportRequestsFromInstances(CancellationToken stoppingToken)
        {
            //Ask cluster for latest item import requests
            var items = await _clusterioService.GetImportRequestsFromInstances(stoppingToken);

            var actualImports = new Dictionary<int, ImportFullfillmentForInstanceJSON>();

            //Add the items
            foreach (var ImportRequestForInstanceDto in items)
            {
                int instanceId = ImportRequestForInstanceDto.InstanceId;
                var vault = await GetVaultForInstance(instanceId.ToString());
                if (vault == null) throw new Exception();
                foreach (var item in ImportRequestForInstanceDto.Items)
                {
                    string name = ((JsonElement)item[0]).ToString();
                    int qty = 0;
                    ((JsonElement)item[1]).TryGetInt32(out qty);
                    var itemStack = new ItemStackDTO() { NameAndQuality = name, Quantity = qty };
                    //Build up a list of what can be imported, subtract from vault storage
                    int actualQuantity = await DecrementItemStack(vault.Id, itemStack);

                    if (!actualImports.ContainsKey(instanceId)) {
                        actualImports.Add(instanceId, new ImportFullfillmentForInstanceJSON() {InstanceId = instanceId });
                    }
                    if (!actualImports[instanceId].Items.ContainsKey(itemStack.NameAndQuality))
                    {
                        actualImports[instanceId].Items.Add(itemStack.NameAndQuality, actualQuantity);
                    }
                    else
                    {
                        actualImports[instanceId].Items[itemStack.NameAndQuality] += actualQuantity;
                    }
                }
            }

            /*var it = new Dictionary<string, int>();
            it.Add("iron-plate:normal", 2);
            actualImports.Add(1176834574, new ImportFullfillmentForInstanceJSON() { 
                InstanceId = 1176834574,
                Items = it
            });*/

            //Send list of imports back to controller
            await _clusterioService.SetActualImportsForInstances(actualImports, stoppingToken);

            await _context.SaveChangesAsync();
        }
    }

    public class VaultProfile : Profile
    {
        public VaultProfile()
        {
            CreateMap<VaultDTO, Vault>().ReverseMap();
        }
    }
}
