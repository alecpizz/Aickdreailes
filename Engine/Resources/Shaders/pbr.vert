#version 330

// Input vertex attributes
in vec3 vertexPosition;
in vec2 vertexTexCoord;
in vec3 vertexNormal;
in vec3 vertexTangent;

// Input uniform values
uniform mat4 mvp;
uniform mat4 matModel;

// Output vertex attributes (to fragment shader)
out vec2 fragTexCoord;
out vec3 fragPos;
out vec3 fragNormal;
out vec3 fragTangent;
out vec3 fragBinormal;
out mat3 TBN;

void main()
{
    // Calculate binormal from vertex normal and tangent
    vec3 vertexBinormal = cross(vertexNormal, vertexTangent);

    // Calculate fragment normal based on normal transformations
    mat3 normalMatrix = transpose(inverse(mat3(matModel)));

    // Calculate fragment position based on model transformations
    fragPos = vec3(matModel * vec4(vertexPosition, 1.0));

    // Send vertex attributes to fragment shader
    fragTexCoord = vertexTexCoord;
    fragNormal = normalize(normalMatrix * vertexNormal);
    fragTangent = normalize(normalMatrix * vertexTangent);
    fragTangent = normalize(fragTangent - dot(fragTangent, fragNormal) * fragNormal);
    fragTangent = vertexTangent;
    fragBinormal = normalize(normalMatrix*vertexBinormal);
    fragBinormal = cross(fragNormal, fragTangent);
    TBN = transpose(mat3(fragTangent, fragBinormal, fragNormal));
    // Calculate final vertex position
    gl_Position = mvp * vec4(vertexPosition, 1.0);
}