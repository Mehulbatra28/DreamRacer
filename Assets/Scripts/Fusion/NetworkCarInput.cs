using Fusion;

public struct NetworkCarInput : INetworkInput
{
    public float Steering;
    public NetworkButtons Buttons;

    public const int ACCELERATE = 0;
    public const int REVERSE = 1;
    public const int BRAKE = 2;
    public const int SHIFT_UP = 3;
    public const int SHIFT_DOWN = 4;
    public const int CLUTCH = 5;
}
