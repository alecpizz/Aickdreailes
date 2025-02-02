#version 330

// Input vertex attributes (from vertex shader)
in vec3 fragPos;

// Input uniform values
uniform sampler2D equirectangularMap;

// Output fragment color
out vec4 finalColor;

vec2 SampleSphericalMap(vec3 v)
{
    vec2 uv = vec2(atan(v.z, v.x), asin(v.y));
    uv *= vec2(0.1591F, 0.3183F);
    uv += 0.5;
    return uv;
}

void main()
{
    // Normalize local position 
    vec2 uv = SampleSphericalMap(normalize(fragPos));

    // Fetch color from texture map
    vec3 color = texture(equirectangularMap, uv).rgb;

    // Calculate final fragment color
    finalColor = vec4(color, 1.0);
}