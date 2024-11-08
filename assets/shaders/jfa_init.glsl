#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 current_pixel = imageLoad(input_image, current_position);

	// The current pixel is not a seed
	if (current_pixel.a == 0.0f) {
		imageStore(output_image, current_position, vec4(0.0f));
		return;
	}

	vec4 packed_position = vec4(unpackUnorm2x16(current_position.x), unpackUnorm2x16(current_position.y));
	imageStore(output_image, current_position, packed_position);
}
