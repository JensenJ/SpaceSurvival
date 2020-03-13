using Unity.Entities;

//A class to hold all data types to do with the Galaxy generation
public static class GalaxyData
{
    //Static function to create star data from the scale given, mainly calculates the type of star based on size.
    public static StarData CreateStarData(float scale)
    {
        StarData starData = new StarData
        {
            starSize = scale
        };

        if(scale > 0.90f)
        {
            starData.starType = StarType.RedSuperGiant;
        }
        else if(scale > 0.75f)
        {
            starData.starType = StarType.RedGiant;
        }
        else if(scale > 0.20f)
        {
            starData.starType = StarType.Main;
        }
        else if(scale > 0.10f)
        {
            starData.starType = StarType.WhiteDwarf;
        }
        else 
        {
            starData.starType = StarType.BlackDwarf;
        }
        return starData;
    }
}

//Data struct to be placed on star entities
public struct StarData : IComponentData
{
    public StarType starType;
    public float starSize;
}

//Enum for star types.
public enum StarType
{
    Main,
    RedGiant,
    RedSuperGiant,
    WhiteDwarf,
    BlackDwarf,
}

//Data struct to be placed on planet entities
public struct PlanetData : IComponentData
{
    public PlanetType planetType;
    public float planetSize;
    public float planetSurfaceTemperature;
    public float planetOrbitTime;
    public float planetOrbitDistance;
    public float planetRotationSpeed;
    public float surfaceAlbedo;
    public float greenhouseEffect;
    public bool isRinged;
}

//Enum for planet types
public enum PlanetType
{
    Terrestrial,
    GasGiant,
    IceGiant,
}