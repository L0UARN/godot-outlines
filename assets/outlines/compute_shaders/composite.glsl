#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba16, set = 0, binding = 0) uniform restrict readonly image2D input_image_1;
layout(rgba16, set = 1, binding = 0) uniform restrict readonly image2D input_image_2;
layout(rgba16, set = 2, binding = 0) uniform restrict writeonly image2D output_image;

void main() {
	ivec2 current_position = ivec2(gl_GlobalInvocationID.xy);
	vec4 pixel_a = imageLoad(input_image_1, current_position);
	vec4 pixel_b = imageLoad(input_image_2, current_position);

	// https://en.wikipedia.org/wiki/Alpha_compositing
	// a over b
	float alpha_final = pixel_a.a + pixel_b.a * (1.0f - pixel_a.a);
	vec3 color_final = (pixel_a.xyz * pixel_a.a + pixel_b.xyz * pixel_b.a * (1.0f - pixel_a.a)) / alpha_final;

	imageStore(output_image, current_position, vec4(color_final, alpha_final));
}
