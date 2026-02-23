from pathlib import Path


class ToolStatusManager:
    _status_file = Path("tool_status.txt")

    @classmethod
    def is_process_running(cls) -> bool:
        if not cls._status_file.exists():
            return False
        return cls._status_file.read_text(encoding="utf-8").strip() == "Running"

    @classmethod
    def set_process_running(cls, is_running: bool) -> None:
        cls._status_file.write_text("Running" if is_running else "Idle", encoding="utf-8")
