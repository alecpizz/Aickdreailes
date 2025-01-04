#version 330

// Input vertex attributes (from vertex shader)
in vec3 fragPos;

// Input uniform values
uniform samplerCube environmentMap;

// Output fragment color
out vec4 finalColor;

void main()
{
    vec3 envColor = texture(environmentMap, fragPos).rgb;
    
    envColor = envColor / (envColor + vec3(1.0));
    envColor = pow(envColor, vec3(1.0 / 2.2)); 
  
    finalColor = vec4(envColor, 1.0);
}