﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hamamatsu.DCAM4;

namespace BehaveAndScan64
{
    // ================================ MyDcamApi ================================

    /// <summary>
    /// Manager class for DCAM-API.  All members and functions are static.
    /// </summary>
    class MyDcamApi
    {
        public static DCAMERR m_lasterr;
        public static Int32 m_devcount;

        public static bool init()
        {
            DCAMAPI_INIT param = new DCAMAPI_INIT(0);

            m_lasterr = dcamapi.init(ref param);
            m_devcount = (m_lasterr.failed() ? 0 : param.iDeviceCount);

            return ! m_lasterr.failed();
        }

        public static bool uninit()
        {
            m_lasterr = dcamapidll.dcamapi_uninit();
            return ! m_lasterr.failed();
        }
    }

    // ================================ MyDcam ================================

    /// <summary>
    /// Heler class for HDCAM
    /// </summary>
    public class MyDcam
    {
        public DCAMERR m_lasterr;
        public IntPtr m_hdcam;
        //public IntPtr m_hdcamrec;
        public DCAMCAP_START m_capmode = DCAMCAP_START.SEQUENCE;

        public bool dev_open( int iCamera )
        {
            DCAMDEV_OPEN    param = new DCAMDEV_OPEN(iCamera);
            m_lasterr = dcamdev.open(ref param);
            if( m_lasterr.failed() )
            {
                m_hdcam = IntPtr.Zero;
            }
            else
            {
                if( m_hdcam != IntPtr.Zero) {
                    dcamdev.close(m_hdcam);
                }
                m_hdcam = param.hdcam;
            }

            return ! m_lasterr.failed();
        }
        public bool dev_close()
        {
            if( m_hdcam == IntPtr.Zero) 
                return true;    // already closed

            m_lasterr = dcamdev.close(m_hdcam);
            if( ! m_lasterr.failed() )
                m_hdcam = IntPtr.Zero;

            return ! m_lasterr.failed();
        }
        public string dev_getstring(DCAMIDSTR iString)
        {
            string ret;
            ret = "";

            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcamdev.getstring(m_hdcam,iString,ref ret);
                if( m_lasterr.failed())
                    ret = "";   // return empty string when error happened.
            }

            return ret;
        }
        // dcamdev_getcapability() is not supported in this code
        // dev_getcapability( ByRef param As DCAMDEV_CAPABILITY) As Integer

        // ---------------- buffer allocation and get ----------------

        public bool buf_alloc(int framecount)
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcambuf.alloc(m_hdcam, framecount);
            }

            return ! m_lasterr.failed();
        }

        public bool buf_attach(int framecount)
        {

            DCAMBUF_ATTACH attach = new DCAMBUF_ATTACH();

            if (m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcambuf.attach(m_hdcam, ref attach);
            }
            

            return !m_lasterr.failed();
        }

        public bool buf_release()
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcambuf.release(m_hdcam, 0);
            }

            return ! m_lasterr.failed();
        }
        public bool buf_lockframe( ref DCAMBUF_FRAME aFrame )
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcambuf.lockframe(m_hdcam, ref aFrame);
            }

            return ! m_lasterr.failed();
        }

        // dcambuf_copyframe(ByVal hdcam As IntPtr, ByRef pFrame As DCAMBUF_FRAME) As Integer
        // dcambuf_copymetadata(ByVal hdcam As IntPtr, ByRef hdr As DCAM_METADATAHDR) As Integer

        // ---------------- capture control ----------------

        public bool cap_start()
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcamcap.start(m_hdcam, m_capmode);
            }

            return ! m_lasterr.failed();
        }

        public bool cap_record(IntPtr m_hdcamrec)
        {
            if (m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcamcap.record(m_hdcam, m_hdcamrec);
            }

            return !m_lasterr.failed();
        }

        public bool cap_stop()
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                
                m_lasterr = dcamcap.stop(m_hdcam);
            }

            return ! m_lasterr.failed();
        }
        public DCAMCAP_STATUS cap_status()
        {
            DCAMCAP_STATUS  stat = DCAMCAP_STATUS.ERROR;
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
           }
            else
            {
                m_lasterr = dcamcap.status(m_hdcam,ref stat);
                if(  m_lasterr.failed() )
                    stat = DCAMCAP_STATUS.ERROR;
            }

            return stat;
        }
        public bool cap_transferinfo(ref int nNewestFrameIndex, ref int nFrameCount )
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                DCAMCAP_TRANSFERINFO    param = new DCAMCAP_TRANSFERINFO(0);
                m_lasterr = dcamcap.transferinfo(m_hdcam, ref param);
                if( ! m_lasterr.failed() )
                {
                    nNewestFrameIndex = param.nNewestFrameIndex;
                    nFrameCount = param.nFrameCount;
                    return true;
                }
            }

            nNewestFrameIndex = -1;
            nFrameCount = 0;
            return false;
        }
        public bool cap_firetrigger()
        {
            if( m_hdcam == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDHANDLE;
            }
            else
            {
                m_lasterr = dcamcap.firetrigger(m_hdcam, 0);
            }

            return ! m_lasterr.failed();
        }
        // dcamcap_record(ByVal hdcam As IntPtr, ByVal hrec As IntPtr) As Integer
    }

    // ================================ MyDcamWait ================================

    /// <summary>
    /// helper class for HDCAMWAIT and dcamwait functions
    /// </summary>
    class MyDcamWait : IDisposable
    {
        public DCAMERR m_lasterr;
        public IntPtr m_hwait;
        public Int32 m_supportevent;
        public Int32 m_timeout;

        public MyDcamWait( ref MyDcam mydcam )
        {
            if( mydcam.m_hdcam == IntPtr.Zero )
            {
                // mydcam should have valid HDCAM handle.
                m_lasterr = DCAMERR.INVALIDHANDLE;
                m_hwait = IntPtr.Zero;
            }
            else
            {
                DCAMWAIT_OPEN   param = new DCAMWAIT_OPEN(0);
                param.hdcam = mydcam.m_hdcam;

                m_lasterr = dcamwait.open(ref param);
                if( !m_lasterr.failed())
                {
                    m_hwait = param.hwait;
                    m_supportevent = param.supportevent;
                }
                else
                {
                    m_hwait = IntPtr.Zero;
                    m_supportevent = 0;
                }
            }

            m_timeout = 1000;        // 1 second
        }

        public void Dispose()
        {
            if( m_hwait != IntPtr.Zero)
            {
                dcamwait.close(m_hwait);
                m_hwait = IntPtr.Zero;
            }
        }

        public bool start(DCAMWAIT eventmask,ref DCAMWAIT eventhappened)
        {
            if( m_hwait == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDWAITHANDLE;
            }
            else
            {
                DCAMWAIT_START  param = new DCAMWAIT_START(eventmask);
                param.timeout = m_timeout;
                m_lasterr = dcamwait.start (m_hwait, ref param);
                if( ! m_lasterr.failed() )
                    eventhappened = new DCAMWAIT(param.eventhappened);
            }

            return ! m_lasterr.failed();
        }

        public bool abort()
        {
            if( m_hwait == IntPtr.Zero)
            {
                m_lasterr = DCAMERR.INVALIDWAITHANDLE;
            }
            else
            {
                m_lasterr = dcamwait.abort(m_hwait);
            }

            return ! m_lasterr.failed();
        }
    }

    // ================================ MyDcamRec ================================

    /// <summary>
    /// helper class for HDCAMREC and dcamrec functions
    /// </summary>
    public class MyDcamRec
    {
        public DCAMERR m_lasterr;
        //public IntPtr m_hdcam;
        public IntPtr m_hdcamrec;
        public IntPtr m_status;
        //public DCAMCAP_START m_capmode = DCAMCAP_START.SEQUENCE;
        public bool open(Int32 record_max_framecount,
                        Int32 fixedfile,
                        Int32 fixedsession,
                        Int32 fixedframe)
        {
            
            

            long max_filesize = (long)(fixedfile)                             // file
                                  + (long)(fixedsession)                    // session
                                  + (long)(fixedframe) * record_max_framecount;
            
            DCAMREC_OPEN param = new DCAMREC_OPEN();
            
            param.path = "C:\\Users\\zwartlab-users\\testrec";
            param.ext = "dcimg";
            param.size = (Int32)max_filesize;///sizeof(param); // replace with max_filesize?
            param.maxframepersession = record_max_framecount;
            
            m_lasterr = dcamrec.open(ref param);
            //DCAMDEV_OPEN param2 = new DCAMDEV_OPEN(iCamera);
            //dcamcap.record(m_hdcam, param.hrec);
            //dcamcap.start(m_hdcam, m_capmode);


            //DCAM_IDPROP_EXPOSURETIME
            //DCAMPROP_TRIGGERSOURCE__EXTERNAL
            //DCAMPROP_TRIGGER_MODE__NORMAL
            //DCAMDEV_OPEN param = new DCAMDEV_OPEN(iCamera);
            //m_lasterr = dcamdev.open(ref param);
            if (m_lasterr.failed())
            {
                m_hdcamrec = IntPtr.Zero;
            }
            else
            {
                if (m_hdcamrec != IntPtr.Zero)
                {
                    dcamrec.close(m_hdcamrec);
                }
                m_hdcamrec = param.hrec;
            }

            return !m_lasterr.failed();
        }

        public bool close()
        {
            if (m_hdcamrec == IntPtr.Zero)
                return true;    // already closed
            m_lasterr = dcamdev.close(m_hdcamrec);
            if (!m_lasterr.failed())
                m_hdcamrec = IntPtr.Zero;

            return !m_lasterr.failed();
        }



        public bool status()
        {
            if (m_hdcamrec == IntPtr.Zero)
            {
                return true;
            }
            DCAMREC_STATUS param = new DCAMREC_STATUS();
            m_lasterr = dcamrec.status(m_hdcamrec, ref param);

            if (!m_lasterr.failed())
                m_status = IntPtr.Zero;

            return !m_lasterr.failed();

        }


                // dcamrec_openA(ByRef param As DCAMREC_OPEN) As Integer
                // dcamrec_status(ByVal hrec As IntPtr, ByRef param As DCAMREC_STATUS) As Integer
                // dcamrec_close(ByVal hrec As IntPtr) As Integer
                // dcamrec_lockframe(ByVal hrec As IntPtr, ByRef pFrame As DCAMBUF_FRAME) As Integer
                // dcamrec_copyframe(ByVal hrec As IntPtr, ByRef pFrame As DCAMBUF_FRAME) As Integer
                // dcamrec_writemetadata(ByVal hrec As IntPtr, ByRef hdr As DCAM_METADATAHDR) As Integer
                // dcamrec_lockmetadata(ByVal hrec As IntPtr, ByRef hdr As DCAM_METADATAHDR) As Integer
                // dcamrec_copymetadata(ByVal hrec As IntPtr, ByRef hdr As DCAM_METADATAHDR) As Integer
                // dcamrec_lockmetadatablock(ByVal hrec As IntPtr, ByRef hdr As DCAM_METADATABLOCKHDR) As Integer
                // dcamrec_copymetadatablock(ByVal hrec As IntPtr, ByRef hdr As DCAM_METADATABLOCKHDR) As Integer
            
    }

    // ================================ MyDcamProp ================================

    /// <summary>
    ///  helper function for DCAM properties
    /// </summary>
    public class MyDcamProp
    {
        public DCAMERR m_lasterr;
        public IntPtr m_hdcam;
        public DCAMIDPROP m_idProp;
        public DCAMPROP_ATTR m_attr;

        public MyDcamProp(MyDcam mydcam, DCAMIDPROP _iprop)
        {
            m_hdcam = mydcam.m_hdcam;
            m_idProp = _iprop;
            m_attr = new DCAMPROP_ATTR(m_idProp);
        }
        public MyDcamProp(IntPtr hdcam, DCAMIDPROP _iprop)
        {
            m_hdcam = hdcam;
            m_idProp = _iprop;
            m_attr = new DCAMPROP_ATTR(m_idProp);
        }
        public MyDcamProp Clone()
        {
            MyDcamProp ret = new MyDcamProp(m_hdcam,m_idProp);
            ret.m_attr = m_attr;
            return ret;
        }
        public bool update_attr()
        {
            m_attr.iProp = m_idProp;
            m_lasterr = dcamprop.getattr(m_hdcam,ref m_attr);
            return ! m_lasterr.failed();
        }

        public bool getvalue(ref double value)
        {
            m_lasterr = dcamprop.getvalue(m_hdcam, m_idProp, ref value);
            return ! m_lasterr.failed();
        }
        public bool setvalue(double value)
        {
            m_lasterr = dcamprop.setvalue(m_hdcam, m_idProp, value);
            return ! m_lasterr.failed();
        }
        public bool setgetvalue(ref double value)
        {
            DCAMPROPOPTION   _option = DCAMPROPOPTION.NONE;
            m_lasterr = dcamprop.setgetvalue(m_hdcam, m_idProp, ref value, _option);
            return ! m_lasterr.failed();
        }
        public bool queryvalue(ref double value, DCAMPROPOPTION _option)
        {
            m_lasterr = dcamprop.queryvalue(m_hdcam, m_idProp, ref value, _option);
            return ! m_lasterr.failed();
        }
        public bool queryvalue_next(ref double value)
        {
            m_lasterr = dcamprop.queryvalue(m_hdcam, m_idProp, ref value, DCAMPROPOPTION.NEXT);
            return ! m_lasterr.failed();
        }
        public bool nextid()
        {
            m_lasterr = dcamprop.getnextid(m_hdcam, ref m_idProp, 0);
            return ! m_lasterr.failed();
        }
        public string getname()
        {
            string name = "";
            m_lasterr = dcamprop.getname(m_hdcam, m_idProp, ref name);
            if( m_lasterr.failed() )
            {
                name = "";
            }
            return name;
        }

        public string getvaluetext(double value)
        {
            string ret;
            ret = "";

            m_lasterr = dcamprop.getvaluetext(m_hdcam, m_idProp, value, ref ret);
            if( m_lasterr.failed())
            {
                ret = value.ToString();
            }
            return ret;
        }

        public bool is_attrtype_mode()
        {
            DCAMPROPATTRIBUTE   attr = new DCAMPROPATTRIBUTE(m_attr.attribute);
            if( attr.is_type(DCAMPROPATTRIBUTE.TYPE_MODE))
                return true;

            return false;
        }
        public bool is_attr_readonly()
        {
            DCAMPROPATTRIBUTE   attr = new DCAMPROPATTRIBUTE(m_attr.attribute);
            if( attr.has_attr(DCAMPROPATTRIBUTE.READABLE)
             && ! attr.has_attr(DCAMPROPATTRIBUTE.WRITABLE))
                return true;
            return false;
        }
    }
}
