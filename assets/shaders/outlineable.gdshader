shader_type spatial;
render_mode unshaded, shadows_disabled, fog_disabled, cull_back;

uniform vec3 outlines_color: source_color = vec3(1.0f);

void fragment() {
	ALBEDO = outlines_color;
}
