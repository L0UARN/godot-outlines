#[compute]
#version 450

// 8 by 8 invocation groups, because apparently invocations groups work best when they have 64 invocations
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Input and output images
layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;

// Shader settings
layout(std430, set = 2, binding = 0) buffer restrict readonly BlurAxisBuffer { bool blur_axis; } blur_axis_buffer;
layout(std430, set = 3, binding = 0) buffer restrict readonly BlurSizeBuffer { int blur_size; } blur_size_buffer;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 current_pixel = imageLoad(input_image, current_position);

	if (current_pixel.a >= 0.9f) {
		imageStore(output_image, current_position, current_pixel);
		return;
	}

	int color_count = 0;
	vec3 color_sum = vec3(0.0f);
	int alpha_count = 0;
	float alpha_sum = 0.0f;

	for (int offset = -blur_size_buffer.blur_size; offset <= blur_size_buffer.blur_size; offset++) {
		ivec2 test_position = current_position + ivec2(blur_axis_buffer.blur_axis ? 0 : offset, blur_axis_buffer.blur_axis ? offset : 0);
		float distance_to_test = distance(current_position, test_position);

		if (distance_to_test > float(blur_size_buffer.blur_size)) {
			continue;
		}

		vec4 test_pixel = imageLoad(input_image, test_position);

		if (test_pixel.a >= 0.1f) {
			color_sum += test_pixel.rgb;
			color_count++;
		}

		alpha_sum += test_pixel.a;
		alpha_count++;
	}

	imageStore(output_image, current_position, vec4(color_sum / float(color_count), alpha_sum / float(alpha_count)));
}
