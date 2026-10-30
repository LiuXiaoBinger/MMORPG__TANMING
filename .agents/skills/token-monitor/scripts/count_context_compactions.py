"""只按 Codex 原始日志的顶层 compacted 事件统计上下文压缩，不保留或输出正文。"""

import argparse
import json
from datetime import datetime
from pathlib import Path


def parse_timestamp(value):
    """将日志中带时区的 ISO8601 时间解析为可输出时间；value 是事件时间。"""
    # 兼容 Python 3.10 对 UTC 后缀 Z 的解析。
    timestamp = datetime.fromisoformat(value.replace('Z', '+00:00'))
    if timestamp.tzinfo is None:
        raise ValueError('Timestamp must include a timezone: ' + value)
    return timestamp


def main():
    """读取当前对话日志的事件元数据，输出从对话开始至今的累计压缩计数。"""
    # 参数只选择当前对话日志及临时输出文件，不修改源日志。
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--session-file', type=Path, required=True)
    parser.add_argument('--output', type=Path)
    arguments = parser.parse_args()
    # 只保存元数据和计数，不保存用户消息、摘要或替换历史。
    session_id = None
    first_timestamp = None
    last_timestamp = None
    session_count = 0
    unreadable_lines = 0
    event_metadata = []
    with arguments.session_file.open(encoding='utf-8-sig') as source:
        # line_number 为实际物理行号，用于定位证据而不输出正文。
        for line_number, line in enumerate(source, start=1):
            if not line.strip():
                continue
            try:
                # record 仅用于当前行解析，读取后不保留正文。
                record = json.loads(line)
                record_time = parse_timestamp(record['timestamp'])
            except (json.JSONDecodeError, KeyError, ValueError):
                unreadable_lines += 1
                continue
            if first_timestamp is None:
                first_timestamp = record_time
            last_timestamp = record_time
            if record.get('type') == 'session_meta':
                session_id = record.get('payload', {}).get('id')
            if record.get('type') != 'compacted':
                continue
            session_count += 1
            event_metadata.append({'line': line_number, 'timestamp': record['timestamp']})
    # 格式未知或记录损坏时保留观测值，但不声称对话累计已完整覆盖。
    coverage_gaps = []
    if session_id is None:
        coverage_gaps.append('未找到 Codex session_meta，无法确认日志格式与会话完整性')
    if unreadable_lines:
        coverage_gaps.append('存在无法解析的日志行：' + str(unreadable_lines))
    if first_timestamp is None:
        coverage_gaps.append('日志中没有可读取的带时间记录')
    # 会话计数同样要求元数据与记录可读；会话外的历史不在本文件统计范围。
    complete_session_count = None
    if not coverage_gaps:
        complete_session_count = session_count
    # result 是唯一输出对象，仅含计数与证据位置。
    result = {
        'source': str(arguments.session_file.resolve()),
        'session_id': session_id,
        'event_type': 'compacted',
        'log_start': None,
        'observed_through': None,
        'session_compaction_count': complete_session_count,
        'observed_session_compaction_count': session_count,
        'unreadable_lines': unreadable_lines,
        'coverage_gaps': coverage_gaps,
        'events': event_metadata,
    }
    if first_timestamp is not None:
        result['log_start'] = first_timestamp.isoformat()
        result['observed_through'] = last_timestamp.isoformat()
    # 序列化只处理上方强约定的元数据字典，不透传原日志载荷。
    rendered = json.dumps(result, ensure_ascii=False, indent=2)
    if arguments.output:
        arguments.output.parent.mkdir(parents=True, exist_ok=True)
        arguments.output.write_text(rendered + '\n', encoding='utf-8')
    print(rendered)


if __name__ == '__main__':
    main()
