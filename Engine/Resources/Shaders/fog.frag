#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragPosition;
in vec3 fragNormal;

// Input uniform values
uniform sampler2D texture0;
uniform vec4 colDiffuse;
uniform vec3 viewPos;
uniform float fogDensity;

// Output fragment color
out vec4 finalColor;

// NOTE: Add here your custom variables

void main()
{
    // Texel color fetching from texture sampler
    vec4 texelColor = texture(texture0, fragTexCoord);

    // NOTE: Implement here your fragment shader code

    // final color is the color from the texture 
    //    times the tint color (colDiffuse)
    //    times the fragment color (interpolated vertex color)
    finalColor = texelColor*colDiffuse*fragColor;

    // Fog calculation
    float dist = length(viewPos - fragPosition);

    // these could be parameters...
    const vec4 fogColor = vec4(0.5, 0.5, 0.5, 1.0);
    //const float fogDensity = 0.16;

    // Exponential fog
    float fogFactor = 1.0/exp((dist*fogDensity)*(dist*fogDensity));

    fogFactor = clamp(fogFactor, 0.0, 1.0);

    finalColor = vec4(0.0f, 1.0f, 0.0f, 1.0f);
}
