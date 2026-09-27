"""開いている Unity エディタへ作業を頼むクライアント（EditorCommandBridge.cs の相方）。

使い方（プロジェクトのルートで実行）:
    python Tools/EditorBridge/bridge.py refresh
    python Tools/EditorBridge/bridge.py execute Namespace.Class.Method
    python Tools/EditorBridge/bridge.py test EditMode
    python Tools/EditorBridge/bridge.py test "PlayMode|InGameSceneTests"

refresh はコンパイルの完了まで待ち、エラーがあれば表示して終了コード 1 を返す。
test は結果の JSON が出るまで待ち、失敗があれば終了コード 1 を返す。
"""

import json
import os
import sys
import time
import uuid

sys.stdout.reconfigure(encoding="utf-8")

BRIDGE_DIR = os.path.join("Temp", "EditorBridge")
REQUEST = os.path.join(BRIDGE_DIR, "request.json")
COMPILE = os.path.join(BRIDGE_DIR, "compile.json")
STATE = os.path.join(BRIDGE_DIR, "state.json")


def read_json(path):
    try:
        with open(path, encoding="utf-8-sig") as f:
            return json.load(f)
    except (OSError, ValueError):
        return None


def mtime(path):
    try:
        return os.path.getmtime(path)
    except OSError:
        return 0.0


def send(command, argument=""):
    os.makedirs(BRIDGE_DIR, exist_ok=True)
    request_id = uuid.uuid4().hex[:8]
    temporary = REQUEST + ".tmp"
    with open(temporary, "w", encoding="utf-8") as f:
        json.dump({"id": request_id, "command": command, "argument": argument}, f)
    os.replace(temporary, REQUEST)
    return request_id


def wait_for(path, timeout, since=0.0):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if os.path.exists(path) and mtime(path) > since:
            data = read_json(path)
            if data is not None:
                return data
        time.sleep(0.5)
    return None


def wait_result(request_id, timeout):
    result = wait_for(os.path.join(BRIDGE_DIR, f"result_{request_id}.json"), timeout)
    if result is None:
        print(f"タイムアウト: エディタが応答しません（{timeout}秒）。Unity が開いているか確認してください")
        sys.exit(2)
    return result


def refresh(timeout):
    compile_before = mtime(COMPILE)
    state_before = mtime(STATE)
    request_id = send("refresh")
    result = wait_result(request_id, 120)
    print(result["message"])

    # コンパイルが始まると state.json が isCompiling=true で書き換わる。しばらく待っても始まらなければ変更なし
    started = False
    deadline = time.time() + 20
    while time.time() < deadline:
        if mtime(COMPILE) > compile_before:
            started = True
            break
        # state.json はドメインの読み込み直しでも書き換わるので、コンパイル中と書かれたときだけ開始とみなす
        if mtime(STATE) > state_before and (read_json(STATE) or {}).get("isCompiling"):
            started = True
            break
        time.sleep(0.5)
    if not started:
        print("コンパイルは走りませんでした（変更なし）")
        return 0

    report = wait_for(COMPILE, timeout, since=compile_before)
    if report is None:
        print(f"タイムアウト: コンパイルが終わりません（{timeout}秒）")
        return 2

    errors = report.get("errors", [])
    print(f"コンパイル完了 {report.get('finishedAt')}: エラー {len(errors)} 件 / 警告 {len(report.get('warnings', []))} 件")
    for error in errors:
        print("  " + error)
    if errors:
        return 1

    # エラーが無ければドメインが読み込み直される。読み込み終わる（state.json が isCompiling=false になる）まで待つ
    deadline = time.time() + timeout
    while time.time() < deadline:
        state = read_json(STATE) or {}
        if mtime(STATE) > compile_before and not state.get("isCompiling", True):
            return 0
        time.sleep(0.5)
    print("警告: ドメインの読み込み完了を確認できませんでした")
    return 0


def execute(method, timeout):
    request_id = send("execute", method)
    result = wait_result(request_id, timeout)
    print(result["message"])
    return 0 if result["success"] else 1


def test(argument, timeout):
    request_id = send("runTests", argument)
    wait_result(request_id, 120)
    report = wait_for(os.path.join(BRIDGE_DIR, f"tests_{request_id}.json"), timeout)
    if report is None:
        print(f"タイムアウト: テスト結果が出ません（{timeout}秒）")
        return 2
    print(f"{report['mode']}: 成功 {report['passed']} / 失敗 {report['failed']} / スキップ {report['skipped']}"
          f"（{report['durationSeconds']:.1f}秒）")
    for failure in report["failures"]:
        print("---- FAILED\n" + failure)
    return 1 if report["failed"] else 0


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    command = sys.argv[1]
    argument = sys.argv[2] if len(sys.argv) > 2 else ""
    timeout = float(os.environ.get("BRIDGE_TIMEOUT", "600"))

    if command == "refresh":
        return refresh(timeout)
    if command == "execute":
        return execute(argument, timeout)
    if command == "test":
        return test(argument or "EditMode", timeout)

    print(f"未知のコマンドです: {command}")
    return 2


if __name__ == "__main__":
    sys.exit(main())
