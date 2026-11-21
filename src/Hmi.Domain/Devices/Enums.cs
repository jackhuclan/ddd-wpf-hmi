namespace Hmi.Domain.Devices;

/// <summary>协议类型：S7 / Modbus RTU / Modbus TCP / 内存模拟</summary>
public enum Protocol
{
    S7,
    ModbusRtu,
    ModbusTcp,
    MemorySim
}

/// <summary>通信模块状态，用于整体在线判定与指数退避重连</summary>
public enum CommunicationState
{
    Offline,
    Connecting,
    Connected,
    Reconnecting,
    Partial
}

/// <summary>点位访问模式</summary>
public enum AccessMode
{
    Read,
    ReadWrite
}
