# Builds the sample library used for the PR 1803 screenshots: python3 make_samples.py <lib-dir>
import base64, json, struct, sys, os
from PIL import Image, ImageDraw, ImageFont
import random

lib = sys.argv[1]
lora = os.path.join(lib, "Models", "Lora")
os.makedirs(lora, exist_ok=True)

def cover(name, c1, c2, label, fmt):
    w, h = 512, 768
    im = Image.new("RGB", (w, h)); d = ImageDraw.Draw(im)
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=tuple(int(c1[i] * (1 - t) + c2[i] * t) for i in range(3)))
    random.seed(name)
    for _ in range(14):
        x, y, r = random.randint(0, w), random.randint(0, h), random.randint(30, 140)
        col = tuple(min(255, int(v * 1.3)) for v in (c1 if random.random() < .5 else c2))
        d.ellipse([x - r, y - r, x + r, y + r], outline=col, width=6)
    try: f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 54)
    except OSError: f = ImageFont.load_default()
    d.text(((w - d.textlength(label, font=f)) / 2, h / 2 - 30), label, font=f, fill=(255, 255, 255))
    path = os.path.join(lib, name)
    im.save(path, fmt, **({"quality": 46} if fmt == "JPEG" else {}))
    return base64.b64encode(open(path, "rb").read()).decode()

def write(name, meta):
    hdr = {"lora_up.weight": {"dtype": "F32", "shape": [1], "data_offsets": [0, 4]}, "__metadata__": meta}
    b = json.dumps(hdr, separators=(",", ":")).encode(); b += b" " * ((8 - len(b) % 8) % 8)
    open(os.path.join(lora, name), "wb").write(struct.pack("<Q", len(b)) + b + b"\0" * 4)

wc = cover("watercolor.jpg", (40, 90, 160), (220, 140, 90), "Watercolor", "JPEG")
neon = cover("neon.png", (20, 10, 60), (200, 40, 160), "Neon City", "PNG")
cm = {"ModelId": None, "ModelName": "Soft Watercolor", "ModelDescription": "Loose watercolor washes.", "Nsfw": False,
      "Tags": ["style", "watercolor"], "ModelType": "LORA", "VersionName": "v2.0", "AuthorUsername": "example",
      "BaseModel": "Illustrious", "Hashes": {"SHA256": "0" * 64}, "TrainedWords": ["wtrclr", "soft wash"], "ThumbnailImageUrl": None}
write("soft_watercolor_v2.safetensors", {"ss_network_module": "networks.lora", "ss_network_dim": "32", "ss_base_model_version": "sdxl_base_v1-0",
      "cover_format": "1", "cover_count": "2", "cover_1": wc, "cover_2": wc, "model_sha256_original": "a" * 64, "tensor_data_sha256": "b" * 64, "cm_info": json.dumps(cm)})
write("neon_city_flux.safetensors", {"modelspec.sai_model_spec": "1.0.0", "modelspec.architecture": "flux-1-dev/lora", "modelspec.title": "Neon City",
      "modelspec.trigger_phrase": "neoncity, rain reflections", "modelspec.thumbnail": "data:image/png;base64," + neon})
write("ink_sketch_sdxl.safetensors", {"ss_network_module": "networks.lora", "ss_base_model_version": "sdxl_base_v1-0", "ss_network_dim": "16",
      "ss_tag_frequency": json.dumps({"10_inksketch": {"inksketch": 40, "monochrome": 22}})})
write("plain_lora.safetensors", {"ss_network_module": "networks.lora"})
