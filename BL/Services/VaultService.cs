using AutoMapper;
using DAL.Data;
using DAL.DBO;
using DTO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Services
{
    public interface IVaultService
    {
        public Task<VaultDTO> GetOrCreateVaultForUser(Guid userId);
        public Task<bool> AddItemsToVault(ExportFromInstanceDTO dto);
    }

    public class VaultService : IVaultService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public VaultService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
            var vault = await _context.Vaults.Where(v => v.Instances.Any(i => i.ClusterInstanceId == instanceId)).FirstOrDefaultAsync();
            return vault;
        }

        public async Task<bool> AddItemsToVault(ExportFromInstanceDTO dto)
        {
            var dbo = await GetVaultForInstance(dto.InstanceId);
            if (dbo == null) return false;

            //Loop items to add
            foreach (var item in dto.Items)
            {
                await UpsertItemStack(dbo.Id, item);
            }
            await _context.SaveChangesAsync();

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

    }

    public class VaultProfile : Profile
    {
        public VaultProfile()
        {
            CreateMap<VaultDTO, Vault>().ReverseMap();
        }
    }
}
