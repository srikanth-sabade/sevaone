import emblem from '../assets/karnataka-emblem.webp'
import kseab from '../assets/dept-logos/kseab.webp'
import rguhs from '../assets/dept-logos/rguhs.webp'
import ksrtc from '../assets/dept-logos/ksrtc.webp'
import bhovi from '../assets/dept-logos/bhovi.webp'
import dwdsc from '../assets/dept-logos/dwdsc.webp'
import revenue from '../assets/dept-logos/revenue.webp'
import emapan from '../assets/dept-logos/emapan.webp'

// Logos found on each body's official site; anything missing falls back to the Karnataka emblem.
const LOGOS: Record<string, string> = {
  'karnataka school examination & assessment board': kseab,
  'karnataka school examination and assessment board': kseab,
  'rajiv gandhi university of health sciences, karnataka': rguhs,
  'karnataka state road transport corporation': ksrtc,
  'karnatka bhovi development corporation limited (kbdcl)': bhovi,
  'department for the empowerment of differently abled and senior citizens': dwdsc,
  'department of legal metrology': emapan,
  'revenue department': revenue,
}

export const deptLogo = (name: string) => LOGOS[name.trim().toLowerCase()]
export const deptLogoOrEmblem = (name: string) => deptLogo(name) ?? emblem
