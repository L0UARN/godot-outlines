#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16, set = 0, binding = 0) uniform restrict readonly image2D input_image_1;
layout(rgba16, set = 1, binding = 0) uniform restrict readonly image2D input_image_2;
layout(rgba16, set = 2, binding = 0) uniform restrict writeonly image2D output_image;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 current_pixel_1 = imageLoad(input_image_1, current_position);
	vec4 current_pixel_2 = imageLoad(input_image_2, current_position);
	imageStore(output_image, current_position, current_pixel_1 + current_pixel_2);
}
