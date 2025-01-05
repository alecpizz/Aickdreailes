#version 330

#define MAX_LIGHTS              4
#define MAX_REFLECTION_LOD      4.0
#define MAX_DEPTH_LAYER         20
#define MIN_DEPTH_LAYER         10
#define LIGHT_DIRECTIONAL       0
#define LIGHT_POINT             1

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
in vec4 fragColor;
in mat3 TBN;

// Inputs
uniform sampler2D albedoMap;
uniform sampler2D normalMap;
uniform sampler2D ormMap;
uniform vec4 colDiffuse;

uniform samplerCube environmentMap;
uniform samplerCube irradianceMap;
uniform samplerCube prefilterMap;
uniform sampler2D brdfLUT;

// Input lighting values
uniform Light lights[MAX_LIGHTS];
uniform vec3 viewPos;

uniform float envLightIntensity = 1.0;
uniform float fogDensity = 0.0;
uniform vec3 fogColor = vec3(0.5, 0.5, 0.5);

// Constants
const float PI = 3.14159265359;

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
    vec3 normalTexture = texture(normalMap, fragTexCoord).xyz * 2.0 - 1.0;
    return normalize(normalTexture * TBN);
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
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

vec3 FresnelSchlickRoughness(float cosTheta, vec3 F0, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

void main()
{
    vec3 albedo = ReadAlbedoMap();

    vec3 ORM = ReadORM();

    float ambientOcclusion = ORM.r;
    float roughness = ORM.g;
    float metallic = ORM.b;

    vec3 normal = ReadNormalMap(1.0);
    vec3 view = normalize(viewPos - fragPos);
    vec3 refl = reflect(-view, normal);

    vec3 F0 = vec3(0.04);
    F0 = mix(F0, albedo, metallic);

    // Scene lighting
    vec3 Lo = vec3(0.0);
    vec3 lightDot = vec3(0.0);

    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (lights[i].enabled == 1)
        {
            vec3 light = vec3(0.0);
            vec3 radiance = lights[i].color.rgb;

            if (lights[i].type == LIGHT_DIRECTIONAL)
            {
                light = -normalize(lights[i].target - lights[i].position);
            }
            else if (lights[i].type == LIGHT_POINT)
            {
                light = normalize(lights[i].position - fragPos);

                float distance = length(lights[i].position - fragPos);
                float attenuation = 1.0 / (distance * distance);

                radiance *= attenuation;
            }
            else
            {
                continue;
            }

            // Cook-Torrance BRDF
            vec3 high = normalize(view + light);

            float NDF = DistributionGGX(normal, high, roughness);
            float G = GeometrySmith(normal, view, light, roughness);
            vec3 F = FresnelSchlick(max(dot(high, view), 0.0), F0);

            vec3 numerator = NDF * G * F;
            float denominator = 4.0 * max(dot(normal, view), 0.0) *
            max(dot(normal, light), 0.0) + 0.0001;

            vec3 BRDF = numerator / denominator;

            // Energy conservation
            vec3 kS = F;
            vec3 kD = vec3(1.0) - kS;

            // Factor metalness
            kD *= 1.0 - metallic;


            // Diffuse lighting
            float NdotL = max(dot(normal, light), 0.0);

            // Figure outgoing light
            Lo += (kD * albedo / PI + BRDF) * radiance * NdotL;
            lightDot += radiance * NdotL + BRDF;
        }

        // Calculate ambient lighting w/ IBL
        vec3 F = FresnelSchlickRoughness(max(dot(normal, view), 0.0), F0, roughness);
        vec3 kS = F;
        vec3 kD = vec3(1.0) - kS;
        kD *= 1.0 - metallic;
        vec3 irradiance = texture(irradianceMap, normal).rgb * envLightIntensity;
        vec3 diffuse = irradiance * albedo;

        // Split-Sum approximation
        vec3 prefilterColor = textureLod(prefilterMap, refl, roughness * MAX_REFLECTION_LOD).rgb;
        vec2 brdf = texture(brdfLUT, vec2(max(dot(normal, view), 0.0), roughness)).rg;
        vec3 reflection = prefilterColor * (F * brdf.x + brdf.y);

        // Final lighting
        vec3 ambient = (kD * diffuse + reflection) * ambientOcclusion;

        // TODO: Add emissive term
        vec3 fragmentColor = ambient + Lo; // + emissive
        
        //fog
        float dist = length(viewPos - fragPos);
        float fogFactor = 1.0/exp((dist*fogDensity)*(dist*fogDensity));
        fogFactor = clamp(fogFactor, 0.0, 1.0);
        fragmentColor = mix(fogColor, fragmentColor, fogFactor);

        // HDR tonemapping
        fragmentColor = fragmentColor / (fragmentColor + vec3(1.0));

        // Gamma correction
        fragmentColor = pow(fragmentColor, vec3(1.0 / 2.2));

        // Output
        finalColor = vec4(fragmentColor, 1.0) * colDiffuse * fragColor;
    }
}