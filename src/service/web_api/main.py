from fastapi import FastAPI
from service.web_api.controllers.sensors.presence_sensor_controller import router as presence_sensor_router

app = FastAPI(title="HomeService API")

# prefix="/api" == RoutePrefixConvention("api") de Program.cs
app.include_router(presence_sensor_router, prefix="/api")