shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float edge_softness = 0.06;
uniform vec4 shadow_color : source_color = vec4(0.02, 0.02, 0.03, 1.0);

void fragment() {
	vec4 scene_color = texture(screen_texture, SCREEN_UV);
	float boundary = 1.0 - progress;
	float mask = smoothstep(boundary + edge_softness, boundary, SCREEN_UV.x);
	COLOR = mix(scene_color, shadow_color, mask);
}
