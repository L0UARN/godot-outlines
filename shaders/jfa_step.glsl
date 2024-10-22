#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;
layout(std430, set = 2, binding = 0) buffer restrict readonly JumpDistance { int jump_distance; } jump_distance_buffer;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);

	// TODO: get the 3x3 matrix of pixels in range of the current pixel by the jump distance
}
