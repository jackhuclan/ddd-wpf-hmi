namespace Hmi.Domain.Devices;

/// <summary>设备仓储契约：设备 → 模块 → 点位三级模型的读写边界</summary>
public interface IDeviceStore
{
    IReadOnlyList<Device> GetAll();
    Device? GetById(Guid id);
    void Update(Device device);
    void ReplaceAll(IEnumerable<Device> devices);
}
