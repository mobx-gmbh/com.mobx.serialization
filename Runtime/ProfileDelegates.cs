namespace MobX.Serialization
{
    public delegate void ProfileChangedDelegate(IProfile profile);

    public delegate void ProfileCreatedDelegate(IProfile profile, ProfileCreationArgs args);

    public delegate void ProfileDeletedDelegate(IProfile profile);

    public delegate void ProfileResetDelegate(IProfile profile);
}