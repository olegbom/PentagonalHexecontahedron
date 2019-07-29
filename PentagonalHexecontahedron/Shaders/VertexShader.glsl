#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 2) in float InstanceCoord;



layout(location = 0) out vec4 fsin_Color;


layout(set = 0, binding = 0) uniform ViewProjectionMatrix
{
    mat4 viewProjection;
};

layout(set = 0, binding = 1) uniform ModelMatrix
{
	mat4 modelMatrix;
};


void main()
{
    
    vec3 worldPosition = vec3(Position, 0) + vec3(0, InstanceCoord, 0);
    gl_Position =  viewProjection * modelMatrix * vec4(worldPosition, 1);
    fsin_Color = Color + vec4(InstanceCoord, 0,0,0);
}