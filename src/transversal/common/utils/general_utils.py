class GenericUtils:
    @staticmethod
    def is_null_or_empty(value: str) -> bool:
        return value is None or value == ""

    @staticmethod
    def is_null_or_white_space(value: str) -> bool:
        return value is None or value == " "