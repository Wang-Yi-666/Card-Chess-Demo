shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform float white_mix : hint_range(0.0, 1.0) = 0.0;
uniform float shatter_progress : hint_range(0.0, 1.0) = 0.0;
uniform float cell_density = 7.0;
uniform float shatter_distance = 18.0;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
	vec2 cell_uv = floor(UV * cell_density);
	float seed = hash(cell_uv);
	float angle = seed * 6.28318530718;
	vec2 direction = vec2(cos(angle), sin(angle));
	vec2 uv_offset = direction * shatter_progress * (shatter_distance * TEXTURE_PIXEL_SIZE);
	vec4 sampled = texture(TEXTURE, UV - uv_offset);

	vec3 whitened = mix(sampled.rgb, vec3(1.0), white_mix);
	float fade = 1.0 - smoothstep(0.0, 1.0, shatter_progress);
	float alpha = sampled.a * fade;
	COLOR = vec4(whitened, alpha);
}
