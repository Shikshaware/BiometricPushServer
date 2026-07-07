using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using BiometricPushServer.Service.Interfaces;

namespace BiometricPushServer.Service
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _uow;

        public DepartmentService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync(int? clientId = null)
        {
            var items = await _uow.Departments.FindAsync(d =>
                d.IsActive && (clientId == null || d.ClientId == clientId));
            return items.Select(MapToDto);
        }

        public async Task<DepartmentDto?> GetByIdAsync(int id)
        {
            var dept = await _uow.Departments.GetByIdAsync(id);
            return dept == null ? null : MapToDto(dept);
        }

        public async Task<DepartmentDto> CreateAsync(DepartmentDto dto)
        {
            var dept = new BioDepartment
            {
                CompanyId = dto.CompanyId,
                ClientId = dto.ClientId,
                Name = dto.Name,
                Code = dto.Code,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };
            await _uow.Departments.AddAsync(dept);
            await _uow.SaveChangesAsync();
            return MapToDto(dept);
        }

        public async Task<DepartmentDto?> UpdateAsync(int id, DepartmentDto dto)
        {
            var dept = await _uow.Departments.GetByIdAsync(id);
            if (dept == null) return null;

            dept.Name = dto.Name;
            dept.Code = dto.Code;
            dept.IsActive = dto.IsActive;
            if (dto.CompanyId.HasValue) dept.CompanyId = dto.CompanyId;
            if (dto.ClientId.HasValue) dept.ClientId = dto.ClientId;

            _uow.Departments.Update(dept);
            await _uow.SaveChangesAsync();
            return MapToDto(dept);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var dept = await _uow.Departments.GetByIdAsync(id);
            if (dept == null) return false;
            _uow.Departments.Remove(dept);
            await _uow.SaveChangesAsync();
            return true;
        }

        private static DepartmentDto MapToDto(BioDepartment d) => new DepartmentDto
        {
            Id = d.Id,
            CompanyId = d.CompanyId,
            ClientId = d.ClientId,
            Name = d.Name,
            Code = d.Code,
            IsActive = d.IsActive
        };
    }

    public class LocationService : ILocationService
    {
        private readonly IUnitOfWork _uow;

        public LocationService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<LocationDto>> GetAllAsync(int? clientId = null)
        {
            var items = await _uow.Locations.FindAsync(l =>
                l.IsActive && (clientId == null || l.ClientId == clientId));
            return items.Select(MapToDto);
        }

        public async Task<LocationDto?> GetByIdAsync(int id)
        {
            var loc = await _uow.Locations.GetByIdAsync(id);
            return loc == null ? null : MapToDto(loc);
        }

        public async Task<LocationDto> CreateAsync(LocationDto dto)
        {
            var loc = new BioLocation
            {
                CompanyId = dto.CompanyId,
                ClientId = dto.ClientId,
                Name = dto.Name,
                Address = dto.Address,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };
            await _uow.Locations.AddAsync(loc);
            await _uow.SaveChangesAsync();
            return MapToDto(loc);
        }

        public async Task<LocationDto?> UpdateAsync(int id, LocationDto dto)
        {
            var loc = await _uow.Locations.GetByIdAsync(id);
            if (loc == null) return null;

            loc.Name = dto.Name;
            loc.Address = dto.Address;
            loc.IsActive = dto.IsActive;
            if (dto.CompanyId.HasValue) loc.CompanyId = dto.CompanyId;
            if (dto.ClientId.HasValue) loc.ClientId = dto.ClientId;

            _uow.Locations.Update(loc);
            await _uow.SaveChangesAsync();
            return MapToDto(loc);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var loc = await _uow.Locations.GetByIdAsync(id);
            if (loc == null) return false;
            _uow.Locations.Remove(loc);
            await _uow.SaveChangesAsync();
            return true;
        }

        private static LocationDto MapToDto(BioLocation l) => new LocationDto
        {
            Id = l.Id,
            CompanyId = l.CompanyId,
            ClientId = l.ClientId,
            Name = l.Name,
            Address = l.Address,
            IsActive = l.IsActive
        };
    }
}
