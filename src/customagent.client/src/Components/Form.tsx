import {
  forwardRef,
  useImperativeHandle,
  useRef,
  type ComponentProps,
  type ComponentPropsWithoutRef,
  type RefObject,
  type SubmitEvent,
} from "react";

export type FormHandle = {
  clear: () => void;
};
type FormProps = ComponentPropsWithoutRef<"form"> & {
  onPost: (data: unknown) => void;
  ref: RefObject<FormHandle | null>;
};

export default function form({ children, onPost, ref, ...props }: FormProps) {
  const formRef = useRef<HTMLFormElement>(null);
  useImperativeHandle(ref, () => {
    return {
      clear() {
        formRef.current!.reset();
      },
    };
  });

  function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const data = Object.fromEntries(formData);
    onPost(data);
  }
  return (
    <form onSubmit={handleSubmit} {...props} ref={formRef}>
      {children}
    </form>
  );
}
