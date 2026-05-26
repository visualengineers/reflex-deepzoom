using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ReFlex.Apps.DeepZoom.Model;

public class DataRepository
{
    public int CurrentIndex { get; set; } = 0;
    
    public ImageRepository Images { get; private init; }
    
    public DataRepository()
    {
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };
        
        Images = JsonConvert.DeserializeObject<ImageRepository>(File.ReadAllText(@"Resources\data.json"), new JsonSerializerSettings
        {
            ContractResolver = contractResolver
        });
        
        Images.ImageData = Images.ImageData.Where((img) => img.IsActive).ToList(); 
    }
}