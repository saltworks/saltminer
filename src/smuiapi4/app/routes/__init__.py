def register_blueprints(app):
    from app.routes.settings import settings_bp
    from app.routes.integrations import integrations_bp
    from app.routes.scanning import scanning_bp
    from app.routes.dashboards import dashboards_bp
    from app.routes.custom_jobs import custom_jobs_bp
    from app.routes.report_templates import report_templates_bp
    from app.routes.ssl import ssl_bp
    from app.routes.auth import auth_bp
    app.register_blueprint(settings_bp)
    app.register_blueprint(integrations_bp)
    app.register_blueprint(scanning_bp)
    app.register_blueprint(dashboards_bp)
    app.register_blueprint(custom_jobs_bp)
    app.register_blueprint(report_templates_bp)
    app.register_blueprint(ssl_bp)
    app.register_blueprint(auth_bp)
