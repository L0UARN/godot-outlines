#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;
layout(std430, set = 2, binding = 0) buffer restrict readonly JumpDistanceBuffer { int jump; } jdb;

const float infinity = 1.0f / 0.0f;

float custom_distance(vec2 a, vec2 b, vec2 pixel_size) {
	vec2 difference = a - b;
	return sqrt(pow(difference.x / pixel_size.x, 2) + pow(difference.y / pixel_size.y, 2));
}

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 current_pixel = imageLoad(input_image, current_position);

	ivec2 image_size = imageSize(input_image);
	vec2 pixel_size = vec2(1.0f) / vec2(image_size);
	vec2 normalized_current_position = vec2(current_position) / vec2(image_size);

	float distance_to_closest_seed = infinity;
	vec4 closest_seed = vec4(infinity, infinity, 0.0f, 0.0f);

	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			ivec2 check_position = ivec2(current_position.x + x * jdb.jump, current_position.y + y * jdb.jump);

			// The pixel to check is outside the input image
			if (check_position.x < 0 || check_position.y < 0 || check_position.x >= image_size.x || check_position.y >= image_size.y) {
				continue;
			}

			vec4 check_pixel = imageLoad(input_image, check_position);

			// The pixel to check is not a seed
			if (isinf(check_pixel.x) || isinf(check_pixel.y)) {
				continue;
			}

			float distance_to_seed = custom_distance(normalized_current_position, check_pixel.xy, pixel_size);

			if (distance_to_seed < distance_to_closest_seed) {
				distance_to_closest_seed = distance_to_seed;
				closest_seed = check_pixel;
			}
		}
	}

	imageStore(output_image, current_position, closest_seed);
}
