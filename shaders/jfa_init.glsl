#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;

const float infinity = 1.0f / 0.0f;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 current_pixel = imageLoad(input_image, current_position);
	vec2 normalized_current_position = vec2(current_position) / vec2(imageSize(input_image));

	// The current pixel is not a seed
	if (current_pixel.a < 0.5f) {
		imageStore(output_image, current_position, vec4(infinity, infinity, 0.0f, 0.0f));
		return;
	}

	imageStore(output_image, current_position, vec4(normalized_current_position, 0.0f, 1.0f));
}
