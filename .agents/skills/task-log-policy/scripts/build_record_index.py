"""为四类英文协作记录生成主题、编号与精确行段目录，不输出正文。"""

import argparse
import hashlib
from pathlib import Path


# 相对项目根目录固定的四份源记录，不递归扫描历史或中文版本。
SOURCE_FILES = (
    'Docs/AI/CURRENT_TASK.en.md',
    'Docs/AI/TEST_REPORT.en.md',
    'Docs/AI/DECISIONS.en.md',
    'Docs/AI/QUERY_TASKS.en.md',
)


def build_index(repo_root):
    """按源文件二级标题边界生成目录；行号从一开始，包含标题。"""
    # 目录只保存导航信息，不复制任务正文或判断历史状态。
    output_lines = [
        '# Collaboration record directory',
        '',
        'At the start of every task, search this directory by topic/ID, then read only the listed source line range.',
        'Ranges are inclusive and include the section heading. Do not load all four source files.',
        'At the end of every task, after approved record writes, refresh this directory once (even without new logs):',
        '`python .agents/skills/task-log-policy/scripts/build_record_index.py`.',
        'Use `--check` to verify freshness; generation reads local files without sending their bodies into agent context.',
        'If a heading/range is stale, regenerate before reading. Historical status is not current status.',
        '',
    ]
    # 每个文件只写一次路径和摘要，标题行兼作主题及唯一编号检索项。
    for source_index in range(len(SOURCE_FILES)):
        source_path = SOURCE_FILES[source_index]
        source_bytes = (repo_root / source_path).read_bytes()
        source_lines = source_bytes.decode('utf-8-sig').splitlines()
        heading_offsets = [offset for offset in range(len(source_lines)) if source_lines[offset].startswith('## ')]
        # 摘要按行内容规范化，避免 Windows/Git 换行转换造成伪过期。
        source_digest = hashlib.sha256('\n'.join(source_lines).encode('utf-8')).hexdigest()
        output_lines.extend([
            '## ' + source_path,
            '',
            'Source normalized SHA256: `' + source_digest + '`',
            '',
            '| Topic / record ID / title | Lines |',
            '|---|---|',
        ])
        # 截止行取下一二级标题的上一行；末段读到文件末尾。
        for heading_index in range(len(heading_offsets)):
            start_offset = heading_offsets[heading_index]
            end_line = len(source_lines)
            if heading_index + 1 < len(heading_offsets):
                end_line = heading_offsets[heading_index + 1]
            section_title = source_lines[start_offset][3:].replace('|', '\\|')
            output_lines.append(f'| {section_title} | {start_offset + 1}-{end_line} |')
        output_lines.append('')
    output_lines.extend([
        '## Older records',
        '',
        'Only if the topic/ID is absent above, search `项目修改日志/协作记录归档/2026-10-26/INDEX.md`.',
        'Read only the matching snapshot line range; do not load the full archive.',
        '',
    ])
    return '\n'.join(output_lines).encode('utf-8')


def main():
    """默认更新目录；检查模式只比较内容，过期时返回失败且不写入。"""
    # 参数仅选择项目路径与只读检查，不改变四份源记录。
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument('--check', action='store_true')
    arguments = parser.parse_args()
    index_path = arguments.repo / 'Docs/AI/RECORD_INDEX.md'
    expected_bytes = build_index(arguments.repo)
    if arguments.check:
        if not index_path.exists():
            print('Record directory missing; regenerate it.')
            return 1
        if index_path.read_text(encoding='utf-8') != expected_bytes.decode('utf-8'):
            print('Record directory stale; regenerate it.')
            return 1
        print('Record directory is current.')
        return 0
    index_path.write_bytes(expected_bytes)
    print('Updated Docs/AI/RECORD_INDEX.md (titles and line ranges only).')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
