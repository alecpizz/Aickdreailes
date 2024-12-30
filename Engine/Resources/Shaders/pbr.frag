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
uniform vec3 viewPos;

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

void main()
{
    vec3 albedo = ReadAlbedoMap();
    //albedo = vec3(0.75); // (solid color for testing)

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

            // Cook-Torrance BRDF
            vec3 high = normalize(view + light);

            float NDF = DistributionGGX(normal, high, roughness);
            float G = GeometrySmith(normal, view, light, roughness);
            vec3 F = FresnelSchlick(max(dot(high, view), 0.0), F0);

            vec3 numerator = NDF * G * F;
            float denominator = 4.0 * max(dot(normal, view), 0.0) * 
                max(dot(normal, light), 0.0) + 0.001;

            vec3 BRDF = numerator / denominator;

            // Energy conservation
            vec3 kS = F;
            vec3 kD = vec3(1.0) - kS;

            // Factor metalness
            kD *= 1.0 - metallic;

            // Diffuse lighting
            float NdotL = max(dot(normal, light), 0.0);

            // Figure outgoing light
            Lo = (kD * albedo / PI + BRDF) * radiance * NdotL * lights[i].color.a;
            lightDot += radiance * NdotL + BRDF * lights[i].color.a;
        }

        // Calculate ambient lighting w/ IBL
        vec3 F = FresnelSchlickRoughness(max(dot(normal, view), 0.0), F0, roughness);

        vec3 kS = F;
        vec3 kD = 1.0 - kS;
        kD *= 1.0 - metallic;

        // Indirect diffuse
        vec3 irradiance = texture(irradianceMap, fragNormal).rgb;
        vec3 diffuse = albedo * irradiance;

        // TODO: Specular IBL w/ Split-Sum approximation

        // Temporary output
        // ...
        vec3 ambient = (kD * diffuse) * ambientOcclusion;

        // TODO: Add emissive term
        vec3 fragmentColor = ambient + Lo; // + emissive

        // HDR tonemapping
        fragmentColor = fragmentColor / (fragmentColor + vec3(1.0));

        // Gamma correction
        fragmentColor = pow(fragmentColor, vec3(1.0 / 2.2));

        // Output
        finalColor = vec4(fragmentColor, 1.0);
    }
}