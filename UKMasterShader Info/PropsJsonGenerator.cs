#if false

using System.Linq;
using Newtonsoft.Json;

var shader = DefaultReferenceManager.Instance.masterShader;

var data = new
{
    shader.name,
    properties = Enumerable.Range(0, shader.GetPropertyCount())
    .Select(i => new
    {
        name = shader.GetPropertyName(i),
        type = shader.GetPropertyType(i).ToString(),
        index = i,
        description = shader.GetPropertyDescription(i),
        attributes = shader.GetPropertyAttributes(i)
    })
};

UnityEngine.GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(data);

#endif