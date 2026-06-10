from flask import Flask
from dotenv import load_dotenv
from app.config import Config
from app.routes import register_blueprints

load_dotenv()


def create_app(config_override=None):
    app = Flask(__name__)
    app.config.from_object(Config)
    if config_override:
        app.config.update(config_override)
    register_blueprints(app)

    if not app.config.get("TESTING"):
        from app.services.paths_service import seed_defaults_if_missing
        seed_defaults_if_missing()

    return app
