// Example of a simple additive shader
shader_type spatial;
render_mode blend_mix; // You can try other blend modes here

uniform sampler2D sprite_texture;

void fragment() {
    vec4 tex_color = texture(sprite_texture, UV);
    ALBEDO = tex_color.rbg, ALPHA;
}