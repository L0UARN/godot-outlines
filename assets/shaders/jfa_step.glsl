#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba16, set = 1, binding = 0) uniform restrict writeonly image2D output_image;
layout(std430, set = 2, binding = 0) buffer restrict readonly JumpDistanceBuffer { int jump; } jdb;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	ivec2 image_size = imageSize(input_image);

	float distance_to_closest_seed = 1.0f / 0.0f;
	vec4 closest_seed = vec4(unpackUnorm2x16(0), unpackUnorm2x16(0));

	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			ivec2 check_position = ivec2(current_position.x + x * jdb.jump, current_position.y + y * jdb.jump);

			// The pixel to check is outside the input image
			if (check_position.x < 0 || check_position.y < 0 || check_position.x >= image_size.x || check_position.y >= image_size.y) {
				continue;
			}

			vec4 check_pixel = imageLoad(input_image, check_position);
			vec2 check_seed_position = vec2(float(packUnorm2x16(check_pixel.xy)), float(packUnorm2x16(check_pixel.zw)));

			// The pixel to check is not a seed
			if (check_seed_position.x == 0.0f && check_seed_position.y == 0.0f) {
				continue;
			}

			float distance_to_seed = distance(vec2(current_position), check_seed_position);

			if (distance_to_seed < distance_to_closest_seed) {
				distance_to_closest_seed = distance_to_seed;
				closest_seed = check_pixel;
			}
		}
	}

	imageStore(output_image, current_position, closest_seed);
}
