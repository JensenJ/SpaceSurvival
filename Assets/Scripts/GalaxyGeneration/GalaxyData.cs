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
            starData.starTemperature = 4700;
        }
        else if(scale > 0.75f)
        {
            starData.starType = StarType.RedGiant;
            starData.starTemperature = 5000;
        }
        else if(scale > 0.20f)
        {
            starData.starType = StarType.Main;
            starData.starTemperature = 18000000;
        }
        else if(scale > 0.10f)
        {
            starData.starType = StarType.WhiteDwarf;
            starData.starTemperature = 100000;
        }
        else 
        {
            starData.starType = StarType.BlackDwarf;
            starData.starTemperature = 0;
        }
        return starData;
    }
}

//Data struct to be placed on star entities
public struct StarData : IComponentData
{
    public StarType starType;
    public float starSize;
    public float starTemperature; //Kelvin temperatures used
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
    public float planetOrbitSpeed;
    public float planetOrbitDistance;
    public float planetRotationSpeed;
    public bool isRinged;
}

//Enum for planet types
public enum PlanetType
{
    Terrestrial,
    GasGiant,
    IceGiant,
}