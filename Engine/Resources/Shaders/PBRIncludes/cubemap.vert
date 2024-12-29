#version 330

// Input vertex attributes
in vec3 vertexPosition;

// Input uniform values
uniform mat4 matView;
uniform mat4 matProjection;

// Output vertex attributes (to fragment shader)
out vec3 fragPos;

void main()
{
    fragPos = vertexPosition;
    gl_Position = matProjection * matView * vec4(vertexPosition, 1.0);
}