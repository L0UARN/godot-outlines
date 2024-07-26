#[compute]
#version 450

// 8 by 8 invocation groups, because apparently invocations groups work best when they have 64 invocations
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Input and output images
layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;

// Shader settings
layout(std430, set = 3, binding = 0) buffer restrict readonly OutlineSizeBuffer { int outline_size; } outline_size_buffer;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	float current_pixel = imageLoad(input_image, current_position).a;

	if (current_pixel >= 0.1f) {
		imageStore(output_image, current_position, vec4(0.0f));
		return;
	}

	float min_edge_distance = -1.0f;

	for (int y = -outline_size_buffer.outline_size; y <= outline_size_buffer.outline_size; y++) {
		for (int x = -outline_size_buffer.outline_size; x <= outline_size_buffer.outline_size; x++) {
			ivec2 test_position = current_position + ivec2(x, y);
			float distance_to_test = distance(current_position, test_position);

			if (distance_to_test > float(outline_size_buffer.outline_size)) {
				continue;
			}

			float test_pixel = imageLoad(input_image, test_position).a;
			if (test_pixel <= 0.1f) {
				continue;
			}

			if (min_edge_distance >= 0.0f && distance_to_test >= min_edge_distance) {
				continue;
			}

			min_edge_distance = distance_to_test;
		}
	}

	if (min_edge_distance < 0.0f) {
		imageStore(output_image, current_position, vec4(0.0f));
		return;
	}

	imageStore(output_image, current_position, vec4(1.0f));
}
