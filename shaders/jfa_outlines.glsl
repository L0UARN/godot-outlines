#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;
layout(std430, set = 2, binding = 0) buffer restrict readonly OutlinesSizeBuffer { int size; } osb;

float custom_distance(vec2 a, vec2 b, vec2 pixel_size) {
	vec2 difference = a - b;
	return sqrt(pow(difference.x / pixel_size.x, 2) + pow(difference.y / pixel_size.y, 2));
}

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 current_pixel = imageLoad(input_image, current_position);

	// The current pixel is not part of the rough outline
	if (current_pixel.x == 0.0f && current_pixel.y == 0.0f) {
		imageStore(output_image, current_position, vec4(0.0f));
		return;
	}

	vec2 image_size = vec2(imageSize(input_image));
	vec2 pixel_size = vec2(1.0f) / image_size;

	vec2 normalized_current_position = vec2(current_position) / image_size;
	float distance_to_seed = custom_distance(normalized_current_position, current_pixel.xy, pixel_size);
	float outlinesSize = float(osb.size);

	// The current pixel is outside the outline range
	if (distance_to_seed > outlinesSize) {
		imageStore(output_image, current_position, vec4(0.0f));
		return;
	}

	// The current pixel is on the edge of the outline, outer side
	if (distance_to_seed > outlinesSize - 1.0f) {
		float smoothing = 1.0f - (distance_to_seed - (outlinesSize - 1.0f));
		imageStore(output_image, current_position, vec4(vec3(1.0f), smoothing));
		return;
	}

	// The current pixel is part of the object to outline
	if (distance_to_seed < 0.1f) {
		imageStore(output_image, current_position, vec4(0.0f));
		return;
	}

	// The current pixel is on the edge of the outline, inner side
	if (distance_to_seed < 1.0f) {
		float smoothing = distance_to_seed;
		imageStore(output_image, current_position, vec4(vec3(1.0f), smoothing));
		return;
	}

	// The current pixel is part of the outline
	imageStore(output_image, current_position, vec4(1.0f));
}
