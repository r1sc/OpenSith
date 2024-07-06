int _Ijim;
int _GeoMode;
int _LightMode;
int _ZWrite;
sampler2D _MainTex;
fixed4 _AmbientLight;
fixed4 _ExtraLight;
fixed4 _Tint;
fixed4 _FaceExtraLight;

struct Input 
{
    float2 uv_MainTex : TEXCOORD0;
    float4 color : COLOR;
    float3 normal: NORMAL;
    float3 worldPos : POSITION; // don't change the name as using this name apparently automatically converts vertex pos to world space

};

struct SithSurface
{
    fixed3 Color; // vertex Color
    fixed3 Albedo;
    fixed3 Normal;
    fixed3 Emission;
    half Specular;
    fixed Gloss;
    fixed Alpha;
};

void vert(inout appdata_full v, out Input o)
{
    o.uv_MainTex = v.texcoord;
    o.color      = v.color;
    o.normal     = v.normal;
    o.worldPos   = v.vertex;
}

fixed4 applyTint(fixed4 c)
{
    if (_Tint.r != 0.0 || _Tint.g != 0.0 || _Tint.b != 0.0) {
        c.r += c.r * (_Tint.r - (0.5 * (_Tint.g + _Tint.b)));
        c.g += c.g * (_Tint.g - (0.5 * (_Tint.r + _Tint.b)));
        c.b += c.b * (_Tint.b - (0.5 * (_Tint.g + _Tint.r)));
    }
    return c;
}

fixed4 lcolor(Input i)
{
    fixed4 c = fixed4(1, 1, 1, 1);
    switch (_LightMode)
    {
    case 0: // None
    case 1: // Lit
        break;
    case 2: // Diffuse
        if (_Ijim) {
            c = _FaceExtraLight + _ExtraLight + _AmbientLight;
        } else { // JKDF2 and MOTS
            c = clamp(_FaceExtraLight, _AmbientLight + _ExtraLight, 1);
        }
        c = applyTint(c);
        break;
    case 3: //Gouraud
        if (_Ijim) {
            c = i.color + _FaceExtraLight + _ExtraLight + _AmbientLight;
        } else { // JKDF2 and MOTS
            c = clamp(i.color + _FaceExtraLight, _AmbientLight + _ExtraLight, 1);
        }
        c = applyTint(c);
        break;
    }
    return clamp(c, 0, 1);
}

void surf(Input i, inout SithSurface o) 
{
    if (_GeoMode == 4) // Texture
    {
        fixed4 c = tex2D(_MainTex, i.uv_MainTex);
        o.Albedo = c.rgb;
        o.Alpha  = c.a;
        o.Color  = i.color;
    }
    else if (_GeoMode == 3) // Solid
    {
        o.Albedo = lcolor(i).rgb;
        o.Alpha  = 1.0;
    }
}

void fcolor(Input i, SithSurface o, inout fixed4 color)
{
    if (_GeoMode == 4) { // texture
        color *= lcolor(i);
    }
}

fixed4 LightingBakedLight(SithSurface s, fixed3 lightDir, fixed atten) 
{
    return fixed4(s.Albedo, s.Alpha);
}

float2 CalculateCeilingSkyDistance(float TextureWidth, float TextureHeight, float3 Position, float3 CameraPosition, float3 Normal, float2 CeilingSkyOffset, float CeilingZ)
{
    // Note, multiply by 10 is done to match mesh scaling in engine
    float3 vert = Position - CameraPosition;
    float3 skyVert = normalize(vert);
    
    float3 ceiling = (0, 0, CeilingZ * 10);    
    float3 diff = CameraPosition - ceiling;

    float distanceToSphere = dot(diff, Normal);
    float dott = -dot(skyVert, Normal);
    
    float sphereHitDistance = distanceToSphere / dott;
    
    skyVert *= sphereHitDistance;
    skyVert += CameraPosition;
    
    float invWidth = 1 / (TextureWidth * 10);
    float invHeight = 1 / (TextureHeight * 10);
    
    float u = invWidth *  skyVert.x * 16 + CeilingSkyOffset.x * 10;
    float v = invHeight * skyVert.z * 16 + CeilingSkyOffset.y * 10;

    return float2(u, v);
}