﻿#version 330

// Input vertex attributes (from vertex shader)
in vec3 fragPosition;

// Input uniform values
uniform samplerCube environmentMap;

out vec4 finalColor;

void main()
{
    finalColor = vec4(texture(environmentMap, fragPosition).rgb, 1.0);
}