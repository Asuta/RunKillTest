import os
import uuid
import json
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.staticfiles import StaticFiles
from fastapi.responses import FileResponse
from typing import List

app = FastAPI()

# 气动的命令是   uvicorn main:app --reload


# 1. 创建一个存数据的文件夹
UPLOAD_DIR = "level_data"
if not os.path.exists(UPLOAD_DIR):
    os.makedirs(UPLOAD_DIR)

# 挂载静态文件目录，这样你可以直接通过 URL 访问图片
app.mount("/static", StaticFiles(directory=UPLOAD_DIR), name="static")

# --- 功能 1: 上传关卡 (JSON + 图片) ---
@app.post("/upload_level/")
async def upload_level(
    name: str = Form(...),          # 关卡名字
    json_file: UploadFile = File(...), # 关卡数据文件
    image_file: UploadFile = File(...) # 缩略图文件
):
    # 生成一个唯一的 ID (UUID)，避免文件名冲突
    level_id = str(uuid.uuid4())
    
    # 保存 JSON
    json_filename = f"{level_id}.json"
    json_path = os.path.join(UPLOAD_DIR, json_filename)
    with open(json_path, "wb") as f:
        f.write(await json_file.read())
        
    # 保存图片
    # 这里的图片后缀假设是 png，你也可以动态获取
    img_filename = f"{level_id}.png"
    img_path = os.path.join(UPLOAD_DIR, img_filename)
    with open(img_path, "wb") as f:
        f.write(await image_file.read())
        
    return {"status": "success", "level_id": level_id, "message": "上传成功"}

# --- 功能 2: 获取关卡列表 (分页，带缩略图URL) ---
@app.get("/get_levels/")
def get_levels(page: int = 1, page_size: int = 10):
    # 扫描文件夹里所有的 json 文件
    all_files = os.listdir(UPLOAD_DIR)
    json_files = [f for f in all_files if f.endswith(".json")]
    
    # 按照文件创建时间排序（最新的在前面）
    json_files.sort(key=lambda f: os.path.getctime(os.path.join(UPLOAD_DIR, f)), reverse=True)
    
    # 简单的分页逻辑
    start = (page - 1) * page_size
    end = start + page_size
    paged_files = json_files[start:end]
    
    result = []
    for f in paged_files:
        l_id = f.replace(".json", "")
        json_path = os.path.join(UPLOAD_DIR, f)
        
        # 读取JSON文件获取元数据
        try:
            with open(json_path, 'r', encoding='utf-8') as file:
                level_data = json.load(file)
                save_time = level_data.get("saveTime", "")
                name = level_data.get("name", "")
                object_count = level_data.get("objectCount", 0)
        except (FileNotFoundError, json.JSONDecodeError, KeyError):
            # 如果读取失败，使用默认值
            save_time = ""
            name = ""
            object_count = 0
        
        result.append({
            "level_id": l_id,
            "name": name,
            "save_time": save_time,
            "object_count": object_count,
            "json_url": f"/download_json/{l_id}",
            "thumbnail_url": f"/static/{l_id}.png" # 图片的直接访问链接
        })
        
    return result

# --- 功能 3: 下载指定的 JSON 数据 ---
@app.get("/download_json/{level_id}")
def download_json(level_id: str):
    file_path = os.path.join(UPLOAD_DIR, f"{level_id}.json")
    if os.path.exists(file_path):
        return FileResponse(file_path)
    return {"error": "File not found"}

# 启动命令提示：
# 在命令行输入: uvicorn main:app --reload