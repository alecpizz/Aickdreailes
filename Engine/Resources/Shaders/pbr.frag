#version 330

#define     MAX_LIGHTS              4
#define     MAX_REFLECTION_LOD      4.0
#define     MAX_DEPTH_LAYER         20
#define     MIN_DEPTH_LAYER         10
#define     LIGHT_DIRECTIONAL       0
#define     LIGHT_POINT             1

struct Light {
    int enabled;
    int type;
    vec3 position;
    vec3 target;
    vec4 color;
};

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec3 fragPos;
in vec3 fragNormal;
in vec3 fragTangent;
in vec3 fragBinormal;

// Inputs
uniform sampler2D albedoMap;
uniform sampler2D normalMap;
uniform sampler2D ormMap;

uniform samplerCube environmentMap;
uniform samplerCube irradianceMap;

// Input lighting values
uniform Light lights[MAX_LIGHTS];
uniform vec4 ambient;
uniform vec3 viewPos;

// Constants
const float PI = 3.14159265359;
const vec2 INV_ATAN = vec2(0.1591, 0.3183);
const int SAMPLES = 25;

// Output fragment color
out vec4 finalColor;

vec3 ReadAlbedoMap()
{
    vec3 albedo = texture(albedoMap, fragTexCoord).rgb;
    albedo = pow(albedo, vec3(2.2));

    return albedo;
}

vec3 ReadNormalMap(float intensity)
{
    vec3 tangentNormal = texture(normalMap, fragTexCoord).xyz * 2.0 - 1.0;

    vec3 Q1  = dFdx(fragPos);
    vec3 Q2  = dFdy(fragPos);
    vec2 st1 = dFdx(fragTexCoord);
    vec2 st2 = dFdy(fragTexCoord);

    vec3 N   = normalize(fragNormal);
    N.xy *= intensity;
    N = normalize(N);

    vec3 T  = normalize(Q1 * st2.t - Q2 * st1.t);
    vec3 B  = -normalize(cross(N, T));

    mat3 TBN = mat3(T, B, N);

    return normalize(TBN * tangentNormal);
}

vec3 ReadORM()
{
    return texture(ormMap, fragTexCoord).rgb;
}

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a2 = roughness * roughness;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;

    return (num / denom);
}

float GeometrySchlickGGX(float NdotV, float roughness) 
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return (num / denom);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness) 
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

vec3 FresnelSchlick(float cosTheta, vec3 F0) 
{
    return F0 + (1.0 - F0) * max(1.0 - cosTheta, 0.0);
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0)*pow(1.0 - cosTheta, 5.0);
}

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= INV_ATAN;
    uv *= 0.5;

    return uv;
}

vec3 GatherIrradiance()
{
    vec3 normal = normalize(fragPos);
    
    vec3 irradiance = vec3(0.0);

    vec3 up = vec3(0.0, 1.0, 0.0);
    vec3 right = normalize(cross(up, normal));
    up = normalize(cross(normal, right));

    float sampleDelta = 0.025;
    float nrSamples = 0.0;

    for (float phi = 0.0; phi < 2.0 * PI; phi += sampleDelta)
    {
        for (float theta = 0.0; theta < 0.5 * PI; theta += sampleDelta)
        {
            vec3 tangentSample = vec3(
                sin(theta) * cos(phi), 
                sin(theta) * sin(phi), 
                cos(theta)
            );
            vec3 sampleVec = tangentSample.x * right + tangentSample.y * 
                up + tangentSample.z * normal; 

            irradiance += texture(environmentMap, sampleVec).rgb * cos(theta) * sin(theta);
            nrSamples++;
        }
    }

    return PI * irradiance * (1.0 / float(nrSamples));
}

void main()
{
    vec3 albedo = ReadAlbedoMap();
    //albedo = vec3(0.75); // (solid color for testing)

    vec3 ORM = ReadORM();

    float ambientOcclusion = ORM.r;
    float roughness = ORM.g;
    float metallic = ORM.b;

    vec3 N = ReadNormalMap(1.0);
    vec3 V = normalize(viewPos - fragPos);
    vec3 R = reflect(-V, N);

    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    vec3 Lo = vec3(0.0);
    vec3 lightDot = vec3(0.0);

    // LIGHT LOOP

    // temp: get actual light value
    vec3 L = normalize(vec3(1.0, 1.0, -2.0));
    // TODO: calculate proper radiance
    vec3 radiance = vec3(1.0);

    // Cook-torrance BRDF
    vec3 H = normalize(V + L);
    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 numerator = NDF * G * F;
    float denominator = 4.0 * max(dot(N, V), 0.0) * 
        max(dot(N, L), 0.0) + 0.001;
    vec3 BRDF = numerator / denominator;

    vec3 kS = F;
    vec3 kD = vec3(1.0) - F;
    kD *= 1.0 - metallic;

    float NdotL = max(dot(N, L), 0.0);

    Lo += (kD * albedo / PI + BRDF) * radiance * NdotL;
    lightDot += radiance * NdotL + BRDF;

    // END LIGHT LOOP

    // Calculate ambient lighting (via IBL)

    vec3 irradiance = texture(irradianceMap, fragNormal).rgb;

    kS = FresnelSchlickRoughness(
        max(dot(N, V), 0.0), 
        F0,
        roughness
    );
    kD = 1.0 - kS;

    vec3 diffuse = irradiance * albedo;
    vec3 ambient = (kD * diffuse) * ambientOcclusion;

    vec3 color = ambient + Lo;

    // HDR tonemapping
    color = color / (color + vec3(1.0));
    // Gamma correction
    color = pow(color, vec3(1.0 / 2.2));

    finalColor = vec4(color, 1.0);
}