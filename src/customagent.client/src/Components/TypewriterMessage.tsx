import { useEffect, useMemo, useRef, useState } from "react";
import { MarkdownConverter } from "@syncfusion/ej2-markdown-converter";

// Slowest reveal speed, used when the animation has caught up with the stream.
const MIN_CHARS_PER_SECOND = 60;
// When text is buffered, speed up so the backlog drains in roughly this time.
const CATCH_UP_SECONDS = 0.3;

type TypewriterMessageProps = {
  content: string;
  formattedMessage: string;
  animate: boolean;
  showCursor?: boolean;
  onReveal?: () => void;
};

// Places the cursor inside the innermost trailing element, so it follows the
// last character instead of landing below a closing paragraph or list.
function appendCursor(html: string, cursor: string) {
  return html.replace(/((?:<\/[^>]+>\s*)*)$/, `${cursor}$1`);
}

export default function TypewriterMessage({
  content,
  formattedMessage,
  animate,
  showCursor = false,
  onReveal,
}: TypewriterMessageProps) {
  const [visibleLength, setVisibleLength] = useState(
    animate ? 0 : content.length,
  );
  const visibleLengthRef = useRef(visibleLength);
  const onRevealRef = useRef(onReveal);

  useEffect(() => {
    onRevealRef.current = onReveal;
  }, [onReveal]);

  useEffect(() => {
    if (visibleLengthRef.current >= content.length) {
      return;
    }

    let frame = 0;
    let lastTime = performance.now();
    let pending = 0;

    const tick = (now: number) => {
      const elapsed = (now - lastTime) / 1000;
      lastTime = now;

      const remaining = content.length - visibleLengthRef.current;
      const speed = Math.max(MIN_CHARS_PER_SECOND, remaining / CATCH_UP_SECONDS);
      pending += speed * elapsed;

      const step = Math.min(remaining, Math.floor(pending));
      if (step > 0) {
        pending -= step;
        visibleLengthRef.current += step;
        setVisibleLength(visibleLengthRef.current);
        onRevealRef.current?.();
      }

      if (visibleLengthRef.current < content.length) {
        frame = requestAnimationFrame(tick);
      }
    };

    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [content]);

  const isComplete = visibleLength >= content.length;
  const html = useMemo(() => {
    const body = isComplete
      ? formattedMessage
      : (MarkdownConverter.toHtml(content.slice(0, visibleLength), {
          async: false,
        }) as string);
    if (!showCursor) {
      return body;
    }
    const cursorClass = isComplete ? "cursor" : "cursor cursor-solid";
    return appendCursor(body, `<span class="${cursorClass}"></span>`);
  }, [isComplete, formattedMessage, content, visibleLength, showCursor]);

  return <div className="message" dangerouslySetInnerHTML={{ __html: html }} />;
}
