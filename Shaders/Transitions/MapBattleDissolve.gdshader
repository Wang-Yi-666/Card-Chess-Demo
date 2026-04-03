shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float block_size = 12.0;
uniform float spread = 0.55;
uniform float drift_pixels = 18.0;
uniform vec4 edge_tint : source_color = vec4(0.51, 0.84, 1.0, 1.0);
uniform vec4 background_tint : source_color = vec4(0.02, 0.03, 0.06, 1.0);

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
	vec2 viewport_size = 1.0 / SCREEN_PIXEL_SIZE;
	vec2 block_index = floor((SCREEN_UV * viewport_size) / block_size);
	float block_seed = hash(block_index);
	float local_progress = clamp((progress - block_seed * spread) / max(0.0001, 1.0 - spread), 0.0, 1.0);
	float dissolve = smoothstep(0.0, 1.0, local_progress);

	vec2 drift = vec2(
		hash(block_index + vec2(13.7, 5.3)) - 0.5,
		hash(block_index + vec2(2.9, 29.1)) - 0.5
	);
	float drift_len = max(length(drift), 0.001);
	vec2 drift_dir = drift / drift_len;
	vec2 uv_offset = drift_dir * (drift_pixels / viewport_size) * dissolve;

	vec4 scene_color = texture(screen_texture, clamp(SCREEN_UV - uv_offset, vec2(0.0), vec2(1.0)));

	float edge_band = smoothstep(0.18, 0.55, local_progress) * (1.0 - smoothstep(0.55, 0.95, local_progress));
	vec3 tinted_color = mix(scene_color.rgb, edge_tint.rgb, edge_band);
	vec3 dissolved_color = mix(tinted_color, background_tint.rgb, dissolve);

	COLOR = vec4(dissolved_color, 1.0);
}
