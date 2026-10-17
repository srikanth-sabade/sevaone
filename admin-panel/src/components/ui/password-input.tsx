import { Eye, EyeOff } from 'lucide-react'
import * as React from 'react'
import { cn } from '../../lib/utils'
import { Input } from './input'

type PasswordInputProps = Omit<React.ComponentProps<'input'>, 'type'> & {
  containerClassName?: string
}

export const PasswordInput = React.forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ className, containerClassName, disabled, ...props }, ref) => {
    const [visible, setVisible] = React.useState(false)

    return (
      <div className={cn('relative', containerClassName)}>
        <Input
          ref={ref}
          type={visible ? 'text' : 'password'}
          disabled={disabled}
          className={cn('pr-10', className)}
          {...props}
        />
        <button
          type="button"
          tabIndex={-1}
          disabled={disabled}
          aria-label={visible ? 'Hide password' : 'Show password'}
          onClick={() => setVisible((v) => !v)}
          className="absolute right-2 top-1/2 -translate-y-1/2 rounded-md p-1 text-muted-foreground transition hover:text-foreground disabled:pointer-events-none disabled:opacity-50"
        >
          {visible ? <EyeOff className="h-4 w-4" strokeWidth={1.75} /> : <Eye className="h-4 w-4" strokeWidth={1.75} />}
        </button>
      </div>
    )
  },
)
PasswordInput.displayName = 'PasswordInput'
