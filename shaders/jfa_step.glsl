#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba8, set = 0, binding = 0) uniform restrict readonly image2D input_image;
layout(rgba8, set = 1, binding = 0) uniform restrict writeonly image2D output_image;
layout(std430, set = 2, binding = 0) buffer restrict readonly JumpDistanceBuffer { int jump; } jdb;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	ivec2 image_size = imageSize(input_image);

	ivec2[9] positions_to_check = ivec2[9](
		ivec2(current_position.x - jdb.jump, current_position.y - jdb.jump), ivec2(current_position.x, current_position.y - jdb.jump), ivec2(current_position.x + jdb.jump, current_position.y - jdb.jump),
		ivec2(current_position.x - jdb.jump, current_position.y), ivec2(current_position.x, current_position.y), ivec2(current_position.x + jdb.jump, current_position.y),
		ivec2(current_position.x - jdb.jump, current_position.y + jdb.jump), ivec2(current_position.x, current_position.y - jdb.jump), ivec2(current_position.x + jdb.jump, current_position.y + jdb.jump)
	);

	vec4 min_distance_pixel = vec4(-1.0f);
	for (int i = 0; i < 9; i++) {
		ivec2 position_to_check = positions_to_check[i];

		if (position_to_check.x < 0 || position_to_check.y < 0 || position_to_check.x >= image_size.x || position_to_check.y >= image_size.y) {
			continue;
		}

		vec4 pixel_to_check = imageLoad(input_image, position_to_check);

		if (min_distance_pixel.a == -1.0f) {
			min_distance_pixel = pixel_to_check;
			continue;
		}

		// TODO: distance comparison
	}
}
